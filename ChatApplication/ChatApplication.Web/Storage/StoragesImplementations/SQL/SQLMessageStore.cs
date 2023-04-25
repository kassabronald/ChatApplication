using ChatApplication.Web.Dtos;
using Microsoft.Data.SqlClient;

namespace ChatApplication.Storage.SQL;

public class SQLMessageStore : IMessageStore
{
    
    private readonly SqlConnection _sqlConnection;
    
    public SQLMessageStore(SqlConnection sqlConnection)
    {
        _sqlConnection = sqlConnection;
    }

    public Task AddMessage(Message message)
    {
        throw new NotImplementedException();
    }

    public Task<GetMessagesResult> GetMessages(GetMessagesParameters parameters)
    {
        throw new NotImplementedException();
    }

    public Task DeleteMessage(Message message)
    {
        throw new NotImplementedException();
    }

    public Task<Message> GetMessage(string conversationId, string messageId)
    {
        throw new NotImplementedException();
    }
}