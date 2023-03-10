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
    public Task<UnixTime> AddMessage(Message message)
    {
        return _messageStore.AddMessage(message);
    }
}