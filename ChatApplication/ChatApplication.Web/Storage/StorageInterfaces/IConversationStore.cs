using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public interface IConversationStore
{
    public Task<UserConversation> GetConversation(string username, string conversationId);
    public Task UpdateConversationLastMessageTime(UserConversation userConversation, long lastMessageTime);
    public Task CreateConversation(UserConversation userConversation);

    public Task DeleteConversation(UserConversation userConversation);
    public Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters);
}