using ChatApplication.Exceptions;
using ChatApplication.Web.Dtos;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ChatApplication.Storage.SQL;

public class SQLMessageStore : IMessageStore
{
    private readonly SqlConnection _sqlConnection;

    public SQLMessageStore(SqlConnection sqlConnection)
    {
        _sqlConnection = sqlConnection;
    }

    public async Task AddMessage(Message message)
    {
        var query = @"INSERT INTO Messages (MessageId, SenderUsername, Text, CreatedUnixTime, ConversationId) 
                VALUES (@MessageId, @SenderUsername, @Text, @CreatedUnixTime, @ConversationId)";

        try
        {
            await _sqlConnection.ExecuteAsync(query, message);
        }
        catch (SqlException ex)
        {
            if (ex.Number == 2627) 
            { 
                throw new MessageAlreadyExistsException($"Message with Id {ex.Message} already exists");
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<GetMessagesResult> GetMessages(GetMessagesParameters parameters)
    {
        parameters.Limit = Math.min(parameters.Limit, 100);
        parameters.Limit = Math.max(parameters.Limit, 1);
        var query = @"SELECT * FROM Messages WHERE  ConversationId = @ConversationId
                AND CreatedUnixTime > @lastSeenMessageTime
                ORDER BY CreatedUnixTime DESC
				OFFSET 0 ROWS
				FETCH NEXT @Limit ROWS ONLY
                OPTION (OPTIMIZE FOR UNKNOWN, OPTIMIZE FOR UNKNOWN, OPTIMIZE FOR UNKNOWN)";
    }

    public async Task DeleteMessage(Message message)
    {
        var query = @"DELETE FROM Messages WHERE MessageId = @MessageId";
        try
        {
            await _sqlConnection.ExecuteAsync(query, message);
        }
        catch (SqlException ex)
        {
            //if (!ex.Number ==)
        }
    }

    public async Task<Message> GetMessage(string conversationId, string messageId)
    {
        throw new NotImplementedException();
    }
}