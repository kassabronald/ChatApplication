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
        var conversation = await _conversationStore.GetConversation(message.SenderUsername, message.ConversationId);
        foreach (var participant in conversation.Participants)
        {
            var participantConversation =
                await _conversationStore.GetConversation(participant.Username, message.ConversationId);
            await _conversationStore.UpdateConversationLastMessageTime(participantConversation,
                message.CreatedUnixTime);
        }
        await _messageStore.AddMessage(message);
    }
    public async Task<string> StartConversation(string messageId, string senderUsername, string messageContent, long createdTime,
        List<string> participants) 
    {
        //TODO: Check that user is not sending to himself
        var id = "";
        var sortedParticipants = new List<string>(participants);
        sortedParticipants.Sort();
        var foundSenderUsername = sortedParticipants.Aggregate(false, (current, participant) => current || participant == senderUsername);
        if (!foundSenderUsername)
        {
            throw new ProfileNotFoundException("Sender username not found in participants", senderUsername);
        }
        List<Profile> participantsProfile = new List<Profile>();
        foreach (var participantUsername in sortedParticipants)
        {
            id += "_"+participantUsername;
            var profile = await _profileStore.GetProfile(participantUsername);
            participantsProfile.Add(profile);
        }
        var message = new Message(messageId, senderUsername, messageContent, createdTime, id);
        await _messageStore.AddMessage(message);
        foreach(var participantUsername in sortedParticipants)
        {
            List<Profile> recipients = new List<Profile>(participantsProfile);
            recipients.Remove(participantsProfile.Find(x => x.Username == participantUsername));
            var conversation = new UserConversation(id, recipients, createdTime, participantUsername);
            await _conversationStore.CreateConversation(conversation);
        }
        //TODO: After PR1 handle possible errors
        return id;
    }
    
    public async Task<GetMessagesResult > GetMessages(GetMessagesParameters parameters)
    {
        return await _messageStore.GetMessages(parameters);
    }
    
    public async Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters)
    {
        return await _conversationStore.GetConversations(parameters);
    }

}