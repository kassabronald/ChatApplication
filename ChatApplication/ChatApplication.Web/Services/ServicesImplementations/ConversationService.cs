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
        var conversation = await _conversationStore.GetConversation(message.conversationId);
        await _conversationStore.ChangeConversationLastMessageTime(conversation, message.createdUnixTime);
        await _messageStore.AddMessage(message);
    }
    
    public async Task<List<ConversationMessage> > GetConversationMessages(string conversationId)
    {
        return await _messageStore.GetConversationMessages(conversationId);
    }

    public async Task<string> StartConversation(string messageId, string senderUsername, string messageContent, long createdTime,
        List<string> participants)
    {// TODO: Check both users exist, and that user is not sending to himself
        string id = "";
        foreach (var participantUsername in participants) //TODO: Add to each participant's userconversation
        {
            id += "_"+participantUsername;
        }
        List<Profile> participantsProfile = new List<Profile>();
        foreach (var participantUsername in participants)
        {
            var profile = await _profileStore.GetProfile(participantUsername);
            participantsProfile.Add(profile);
        }
        var message = new Message(messageId, senderUsername, messageContent, createdTime, id);
        await _messageStore.AddMessage(message);
        var conversation = new Conversation(id, participantsProfile, createdTime);
        await _conversationStore.StartConversation(conversation);
        // try
        // {
        //     await _conversationStore.StartConversation(conversation);
        // }
        // catch (Exception)
        // {
        //     await _messageStore.DeleteMessage(message);
        //     throw;
        // }
        return id;
    }
}