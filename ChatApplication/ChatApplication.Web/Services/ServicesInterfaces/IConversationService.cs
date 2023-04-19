using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public interface IConversationService
{
    public Task AddMessage(Message message);

    public Task<GetMessagesResult> GetMessages(GetMessagesParameters parameters);

    public Task<string> StartConversation(StartConversationParameters parameters);
    
    public Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters);
    
    public Task EnqueueAddMessage(Message message);

}