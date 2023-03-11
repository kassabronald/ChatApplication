using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public class ConversationService : IConversationService
{
    private readonly IMessageStore _messageStore;
    
    
    public ConversationService(IMessageStore messageStore)
    {
        _messageStore = messageStore;
    }
    public async Task<UnixTime> AddMessage(Message message)
    {
        var conversation = await _messageStore.GetConversation(message.conversationId);
        DateTimeOffset time = DateTimeOffset.UtcNow;
        await _messageStore.ChangeConversationLastMessageTime(conversation, time.ToUnixTimeSeconds());
        await _messageStore.AddMessage(message);
        return new UnixTime(time.ToUnixTimeSeconds());
    }
    
    public async Task<Message[]> GetConversationMessages(string conversationId)
    {
        return await _messageStore.GetConversationMessages(conversationId);
    }
}