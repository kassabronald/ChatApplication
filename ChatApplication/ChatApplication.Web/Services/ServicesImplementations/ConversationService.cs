using ChatApplication.Exceptions;
using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

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
            if (participant.username != conversation.username)
            {
                var receiverConversation =
                    await _conversationStore.GetConversation(participant.username, message.conversationId);
                await _conversationStore.ChangeConversationLastMessageTime(receiverConversation,
                    message.createdUnixTime);
            }
        }
        await _messageStore.AddMessage(message);
    }
    
    public async Task<List<ConversationMessage> > GetConversationMessages(string conversationId, int limit, string continuationToken, long lastMessageTime)
    {
        return await _messageStore.GetConversationMessages(conversationId, limit, continuationToken, lastMessageTime);
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
}