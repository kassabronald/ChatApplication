using ChatApplication.Configuration;
using ChatApplication.Exceptions;
using ChatApplication.Storage.Models;
using ChatApplication.Web.Dtos;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ChatApplication.Storage.SQL;

public class SQLConversationStore : IConversationStore
{
    
    private readonly string _connectionString;

    public SQLConversationStore(IOptions<SQLSettings> sqlSettings )
    {
        _connectionString = sqlSettings.Value.ConnectionString;
    }
    
    private SqlConnection GetSqlConnection()
    {
        return new SqlConnection(_connectionString);
    }
    public async Task<UserConversation> GetUserConversation(string username, string conversationId)
    {
        var queryConversationTable = "SELECT * FROM Conversations WHERE ConversationId = @conversationId";

        await using var sqlConnection = GetSqlConnection();
        await sqlConnection.OpenAsync();
        
        var conversation = await sqlConnection.QueryFirstOrDefaultAsync<ConversationModel>(queryConversationTable, new {conversationId = conversationId});

        if(conversation == null)
        {
            throw new ConversationNotFoundException($"A conversation with id {conversationId} was not found");
        }

        var queryConversationParticipantsTable = "SELECT Username FROM ConversationParticipants WHERE ConversationId = @conversationId";

        var participantsUsernames = (await sqlConnection.QueryAsync<string>(
            queryConversationParticipantsTable, new {ConversationId = conversationId})).AsList();

        List<Profile> participants = new List<Profile>();

        foreach(var participantUsernames in participantsUsernames)
        {
            var query = "SELECT * FROM Profiles WHERE Username = @Username";
            var profile = await sqlConnection.QueryFirstOrDefaultAsync<Profile>(query, new { Username = participantUsernames });

            if (profile == null)
            {
                throw new ProfileNotFoundException($"A recipient with username {username} was not found");
            }
            participants.Add(profile);
        }
        
        await sqlConnection.CloseAsync();

        return new UserConversation(conversationId, participants, conversation.ModifiedUnixTime, username);

    }

    public async Task UpdateConversationLastMessageTime(UserConversation senderConversation, long lastMessageTime)
    {
        var conversationId = senderConversation.ConversationId;
        
        await using var sqlConnection = GetSqlConnection();
        await sqlConnection.OpenAsync();
        
        var checkQuery = "SELECT COUNT(*) FROM Conversations WHERE ConversationId = @ConversationId";
        int conversationCount = await sqlConnection.ExecuteScalarAsync<int>(checkQuery, new { ConversationId = conversationId });

        if (conversationCount == 0)
        {
            throw new ConversationNotFoundException($"A conversation with id {senderConversation.ConversationId} does not exist");
        }
        
        
        var query = "UPDATE Conversations SET ModifiedUnixTime = @lastMessageTime WHERE ConversationId = @ConversationId";
        
        await sqlConnection.ExecuteAsync(query, new { lastMessageTime, ConversationId = conversationId });
        await sqlConnection.CloseAsync();
    }

    public async Task CreateUserConversation(UserConversation userConversation)
    {
        var queryConversationTable = "INSERT INTO Conversations (ConversationId, ModifiedUnixTime) VALUES (@ConversationId, @ModifiedUnixTime)";
        var queryConversationParticipantsTable = "INSERT INTO ConversationParticipants (ConversationId, Username) VALUES (@ConversationId, @Username)";
        
        await using var sqlConnection = GetSqlConnection();
        await sqlConnection.OpenAsync();

        await using var transaction = sqlConnection.BeginTransaction();
        try
        {
            await sqlConnection.ExecuteAsync(queryConversationTable,
                new
                {
                    ConversationId = userConversation.ConversationId,
                    ModifiedUnixTime = userConversation.LastMessageTime
                }, transaction);
            
            await sqlConnection.ExecuteAsync(queryConversationParticipantsTable,
                new { ConversationId = userConversation.ConversationId, Username = userConversation.Username },
                transaction);
            
            foreach (var recipient in userConversation.Recipients)
            {
                await sqlConnection.ExecuteAsync(queryConversationParticipantsTable,
                    new { ConversationId = userConversation.ConversationId, Username = recipient.Username },
                    transaction);
            }

            transaction.Commit();
        }
        catch (SqlException ex)
        {
            if (ex.Number == 2627)
            {
                transaction.Rollback();
                return;
            }
            transaction.Rollback();
            throw;
        }
        
        await sqlConnection.CloseAsync();
    }

    public async Task DeleteUserConversation(UserConversation userConversation)
    {
        var queryConversationTable = "DELETE FROM Conversations WHERE ConversationId = @ConversationId AND ModifiedUnixTime = @ModifiedUnixTime";
        var queryConversationParticipantsTable = "DELETE FROM ConversationParticipants WHERE ConversationId = @ConversationId AND USERNAME = @Username";

        await using var sqlConnection = GetSqlConnection();

        await sqlConnection.OpenAsync();
        

        try
        {
            await sqlConnection.ExecuteAsync(queryConversationTable,
                new
                {
                    ConversationId = userConversation.ConversationId,
                    ModifiedUnixTime = userConversation.LastMessageTime
                });
            
        }
        catch (Exception ex)
        {
        }
    }


    public async Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters)
    {
        var limit = Math.Min(parameters.Limit, 100);
        limit = Math.Max(limit, 1);
        var parameterNormalized = parameters with { Limit = limit };

        var queryConversationParticipantsTable =
            @"SELECT cp.*
        FROM ConversationParticipants cp
        JOIN Conversations c ON cp.ConversationId = c.ConversationId
        WHERE cp.Username = @Username
        ORDER BY c.ModifiedUnixTime DESC
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
        
        var conversationParticipants = (await sqlConnection.QueryAsync<ConversationParticipantsModel>(queryConversationParticipantsTable,
            new
        {
            Username = parameterNormalized.Username,
            Offset = offset,
            Limit = parameterNormalized.Limit
        })).AsList();
        
        var conversationIds = conversationParticipants.Select(conversationParticipant => 
            conversationParticipant.ConversationId).ToList();
        
        var queryConversationParticipantsRecipients = 
            @"SELECT * FROM ConversationParticipants 
                WHERE ConversationId IN @ConversationIds AND Username != @Username";     
        
        var recipientsConversations = (await sqlConnection.QueryAsync<ConversationParticipantsModel>(queryConversationParticipantsRecipients,
            new
            {
                Username = parameterNormalized.Username,
                ConversationIds = conversationIds
            })).AsList();

        var conversationRecipients = new Dictionary<string, List<Profile>>();
        
        foreach(var recipientConversation in recipientsConversations)
        {
            
            var query = "SELECT * FROM Profiles WHERE Username = @Username";
            var profile = await sqlConnection.QueryFirstOrDefaultAsync<Profile>(query, new { Username = recipientConversation.Username });

            if (profile == null)
            {
                throw new ProfileNotFoundException($"A recipient with username {recipientConversation.Username} was not found");
            }

            if (conversationRecipients.ContainsKey(recipientConversation.ConversationId))
            {
                conversationRecipients[recipientConversation.ConversationId].Add(profile);
            }
            else
            {
                conversationRecipients.Add(recipientConversation.ConversationId, new List<Profile> {profile});
            }
        }
        
        var queryConversationsTable = @"SELECT * FROM Conversations 
                  WHERE ConversationId IN @ConversationIds AND ModifiedUnixTime > @LastSeenConversationTime
                  ORDER BY ModifiedUnixTime DESC ";

        var conversations = (await sqlConnection.QueryAsync<ConversationModel>(
            queryConversationsTable, new
            {
                ConversationIds = conversationIds,
                Offset = offset,
                Limit = parameterNormalized.Limit,
                LastSeenConversationTime = parameterNormalized.LastSeenConversationTime
            })).AsList();
        
        var newOffset = offset + conversations.Count;
        
        var userConversations = conversations.Select(
            conversation => new UserConversation(conversation.ConversationId, 
                conversationRecipients[conversation.ConversationId], conversation.ModifiedUnixTime, 
                parameterNormalized.Username)).ToList();
        
        await sqlConnection.CloseAsync();
        
        string? newOffsetString = conversations.Count < parameterNormalized.Limit ? null : newOffset.ToString();
        return new GetConversationsResult(userConversations, newOffsetString);
    }
}