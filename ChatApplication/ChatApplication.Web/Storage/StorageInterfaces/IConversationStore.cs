using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public interface IConversationStore
{
    public Task<UserConversation> GetUserConversation(string username, string conversationId);
    public Task UpdateConversationLastMessageTime(UserConversation senderConversation, long lastMessageTime);
    public Task CreateUserConversation(UserConversation userConversation);

    public Task DeleteUserConversation(UserConversation userConversation);
    public Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters);
}