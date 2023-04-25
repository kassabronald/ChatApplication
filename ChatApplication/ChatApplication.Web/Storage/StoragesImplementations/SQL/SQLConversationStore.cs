using ChatApplication.Web.Dtos;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ChatApplication.Storage.SQL;

public class SQLConversationStore : IConversationStore
{
    
    private readonly SqlConnection _sqlConnection;
    
    public SQLConversationStore(SqlConnection sqlConnection)
    {
        _sqlConnection = sqlConnection;
    }
    public Task<UserConversation> GetUserConversation(string username, string conversationId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateConversationLastMessageTime(UserConversation senderConversation, long lastMessageTime)
    {
        var conversationId = senderConversation.ConversationId;
        var query = "UPDATE Conversations SET ModifiedUnixTime = @lastMessageTime WHERE ConversationId = @ConversationId";
        
        return _sqlConnection.ExecuteAsync(query, new { lastMessageTime, ConversationId = conversationId });
    }

    public Task CreateUserConversation(UserConversation userConversation)
    {
        //TODO: Transaction to add all conversations
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