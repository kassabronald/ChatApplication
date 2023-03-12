using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public class ConversationService : IConversationService
{
    private readonly IMessageStore _messageStore;
    private readonly IConversationStore _conversationStore;
    public ConversationService(IMessageStore messageStore, IConversationStore conversationStore)
    {
        _messageStore = messageStore;
        _conversationStore = conversationStore;
    }
    public async Task AddMessage(Message message)
    {
        var conversation = await _conversationStore.GetConversation(message.conversationId);
        await _conversationStore.ChangeConversationLastMessageTime(conversation, message.createdUnixTime);
        await _messageStore.AddMessage(message);
    }
    
    public async Task<Message[]> GetConversationMessages(string conversationId)
    {
        return await _messageStore.GetConversationMessages(conversationId);
    }
}