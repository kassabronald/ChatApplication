using ChatApplication.Configuration;
using ChatApplication.Exceptions;
using ChatApplication.Web.Dtos;
using Dapper;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ChatApplication.Storage.SQL;

public class SQLMessageStore : IMessageStore
{
    private readonly string _connectionString;

    public SQLMessageStore(IOptions<SQLSettings> sqlSettings)
    {
        _connectionString = sqlSettings.Value.ConnectionString;
    }
    
    private SqlConnection GetSqlConnection()
    {
        return new SqlConnection(_connectionString);
    }
    

    public async Task AddMessage(Message message)
    {
        var query = @"INSERT INTO Messages (MessageId, SenderUsername, ConversationId, Text, CreatedUnixTime) 
                VALUES (@MessageId, @SenderUsername, @ConversationId, @Text, @CreatedUnixTime)";

        await using var sqlConnection = GetSqlConnection();
        await sqlConnection.OpenAsync();
        
        try
        {
            await sqlConnection.ExecuteAsync(query, message);
        }
        catch (SqlException ex)
        {
            if (ex.Number == 2627) 
            { 
                throw new MessageAlreadyExistsException($"Message with Id {ex.Message} already exists");
            }
            
            throw;
        }
        await sqlConnection.CloseAsync();
    }

    public async Task<GetMessagesResult> GetMessages(GetMessagesParameters parameters)
    {
        var limit = Math.Min(parameters.Limit, 100);
        limit = Math.Max(limit, 1);
        var parameterNormalized = parameters with { Limit = limit };

        var query = @"SELECT * FROM Messages WHERE ConversationId = @ConversationId
            AND CreatedUnixTime > @lastSeenMessageTime
            ORDER BY CreatedUnixTime DESC
            OFFSET @Offset ROWS
            FETCH NEXT @Limit ROWS ONLY";
        
        var offset = 0;
        try
        {
            if (!string.IsNullOrEmpty(parameterNormalized.ContinuationToken))
            {
                offset = Int32.Parse(parameterNormalized.ContinuationToken);
            }
        }
        catch (Exception e)
        {
            throw new ArgumentException("A Bad Continutation token/Offset was passed");
        }
        
        await using var sqlConnection = GetSqlConnection();
        await sqlConnection.OpenAsync();
        
        var messages = (await sqlConnection.QueryAsync<Message>(query, new
        {
            ConversationId = parameterNormalized.ConversationId,
            lastSeenMessageTime = parameterNormalized.LastSeenMessageTime,
            Limit = parameterNormalized.Limit,
            Offset = offset
        })).AsList();

        int newOffset = offset + messages.Count;

        var conversationMessages = messages.Select(message => new 
            ConversationMessage(message.SenderUsername, message.Text, message.CreatedUnixTime)).ToList();

        await sqlConnection.CloseAsync();

        string? newOffsetString = messages.Count < parameterNormalized.Limit ? null : newOffset.ToString();
        return new GetMessagesResult(conversationMessages, newOffsetString);
    }

    public async Task DeleteMessage(Message message)
    {
        
        await using var sqlConnection = GetSqlConnection();
        await sqlConnection.OpenAsync();
        
        var query = @"DELETE FROM Messages WHERE MessageId = @MessageId";
        try
        {
            await sqlConnection.ExecuteAsync(query, message);
        }
        catch (SqlException ex)
        {
            if (ex.Number == 2627)
            {
                return;
            }

            throw;
        }
        
        await sqlConnection.CloseAsync();
    }

    public async Task<Message> GetMessage(string conversationId, string messageId)
    {
        
        await using var sqlConnection = GetSqlConnection();
        await sqlConnection.OpenAsync();
        
        var query = "SELECT * FROM Messages WHERE ConversationId = @ConversationId AND MessageId = @MessageId";
        var message = await sqlConnection.QueryFirstOrDefaultAsync<Message>(query, new { ConversationId = conversationId, MessageId = messageId });

        if (message == null)
        {
            throw new MessageNotFoundException($"A message with id {messageId} does not exist");
        }
        
        await sqlConnection.CloseAsync();
        return message;

    }
}