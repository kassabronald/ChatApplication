using ChatApplication.Exceptions;
using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Newtonsoft.Json;

namespace ChatApplication.Services;

public class ConversationService : IConversationService
{
    private readonly IMessageStore _messageStore;
    private readonly IConversationStore _conversationStore;
    private readonly IProfileStore _profileStore;
    public ConversationService(IMessageStore messageStore, IConversationStore conversationStore, IProfileStore profileStore)
    {
        _messageStore = messageStore;
        _conversationStore = conversationStore;
        _profileStore = profileStore;
    }
    public async Task AddMessage(Message message)
    {
        //TODO: Pass both conversationId (rowkey) and senderUsername (partitionkey)
        var conversation = await _conversationStore.GetConversation(message.senderUsername, message.conversationId);
        await _conversationStore.ChangeConversationLastMessageTime(conversation, message.createdUnixTime);
        foreach(var participant in conversation.participants)
        {
            if (participant.username != message.senderUsername)
            {
                var receiverConversation =
                    await _conversationStore.GetConversation(participant.username, message.conversationId);
                await _conversationStore.ChangeConversationLastMessageTime(receiverConversation,
                    message.createdUnixTime);
            }
        }
        await _messageStore.AddMessage(message);
    }
    public async Task<string> StartConversation(string messageId, string senderUsername, string messageContent, long createdTime,
        List<string> participants)
    {// TODO: Check that user is not sending to himself
        string id = "";
        Boolean foundSenderUsername = false;
        List<string> sortedParticipants = new List<string>(participants);
        sortedParticipants.Sort();
        foreach (var participant in sortedParticipants)
        {
            foundSenderUsername= foundSenderUsername || participant == senderUsername;
        }
        if (!foundSenderUsername)
        {
            throw new ProfileNotFoundException("Sender username not found in participants", senderUsername);
        }
        foreach (var participantUsername in sortedParticipants) //TODO: Add to each participant's userconversation
        {
            id += "_"+participantUsername;
        }
        List<Profile> participantsProfile = new List<Profile>();
        foreach (var participantUsername in sortedParticipants)
        {
            var profile = await _profileStore.GetProfile(participantUsername);
            participantsProfile.Add(profile);
        }
        var message = new Message(messageId, senderUsername, messageContent, createdTime, id);
        await _messageStore.AddMessage(message);
        //TODO: Add to each participant's userconversation
        foreach(var participantUsername in sortedParticipants)
        {
            var conversation = new Conversation(id, participantsProfile, createdTime, participantUsername);
            await _conversationStore.CreateConversation(conversation);
        }
        //TODO: After PR1 handle possible errors
        return id;
    }
    
    public async Task<ConversationMessageAndToken > GetConversationMessages(string conversationId, int limit, string continuationToken, long lastMessageTime)
    {
        var jsonContinuationTokenData = continuationToken;
        if (!String.IsNullOrWhiteSpace(continuationToken))
        {
            var continuationTokenData = new ContinuationTokenDataUtil(continuationToken, new RangeUtil("", "FF"));
            jsonContinuationTokenData = JsonConvert.SerializeObject(new List<ContinuationTokenDataUtil>
                { continuationTokenData });
        }
        var jsonResult =  await _messageStore.GetConversationMessages(conversationId, limit, jsonContinuationTokenData, lastMessageTime);
        if(jsonResult.continuationToken == null)
        {
            return jsonResult;
        }
        var deserializedToken = JsonConvert.DeserializeObject<List<ContinuationTokenDataUtil>>(jsonResult.continuationToken)[0];
        var result = jsonResult with { continuationToken = deserializedToken.token };
        return result;
    }
    
    public async Task<ConversationAndToken> GetAllConversations(string username, int limit, string continuationToken, long lastConversationTime)
    {
        var jsonContinuationTokenData = continuationToken;
        if (!String.IsNullOrWhiteSpace(continuationToken))
        {
            var continuationTokenData = new ContinuationTokenDataUtil(continuationToken, new RangeUtil("", "FF"));
            jsonContinuationTokenData = JsonConvert.SerializeObject(new List<ContinuationTokenDataUtil>
                { continuationTokenData });
        }
        var jsonResult =  await _conversationStore.GetAllConversations(username, limit, jsonContinuationTokenData, lastConversationTime);
        if(jsonResult.continuationToken == null)
        {
            return jsonResult;
        }
        var deserializedToken = JsonConvert.DeserializeObject<List<ContinuationTokenDataUtil>>(jsonResult.continuationToken)[0];
        var result = jsonResult with { continuationToken = deserializedToken.token };
        return result;
    }

}