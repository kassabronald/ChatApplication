using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage.SQL;

public class SQLConversationStore : IConversationStore
{
    public Task<UserConversation> GetUserConversation(string username, string conversationId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateConversationLastMessageTime(UserConversation senderConversation, long lastMessageTime)
    {
        throw new NotImplementedException();
    }

    public Task CreateUserConversation(UserConversation userConversation)
    {
        throw new NotImplementedException();
    }

    public Task DeleteUserConversation(UserConversation userConversation)
    {
        throw new NotImplementedException();
    }

    public Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters)
    {
        throw new NotImplementedException();
    }
}