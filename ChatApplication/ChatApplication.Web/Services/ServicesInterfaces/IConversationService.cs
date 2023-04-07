using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public interface IConversationService
{
    public Task AddMessage(Message message);

    public Task<GetMessagesResult> GetMessages(GetMessagesParameters parameters);

    public Task<string> StartConversation(string messageId, string senderUsername, string messageContent,
        long createdTime, List<string> participants);
    
    public Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters);

}