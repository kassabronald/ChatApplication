using ChatApplication.Exceptions;
using ChatApplication.Storage.Models;
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
    public async Task<UserConversation> GetUserConversation(string username, string conversationId)
    {
        var queryConversationTable = "SELECT * FROM Conversations WHERE ConversationId = @conversationId";
        var conversation = await _sqlConnection.QueryFirstOrDefaultAsync<ConversationModel>(queryConversationTable, new {conversationId = conversationId});

        if(conversation == null)
        {
            throw new ConversationNotFoundException($"A conversation with id {conversationId} was not found");
        }

        var queryConversationParticipantsTable = "SELECT Username FROM ConversationParticipants WHERE ConversationId = @conversationId";

        var participantsUsernames = await _sqlConnection.QueryAsync<string>(queryConversationParticipantsTable, new {ConversationId = conversationId});

        List<Profile> participants = new List<Profile>();

        foreach(var participantUsernames in participantsUsernames)
        {
            var query = "SELECT * FROM Conversations WHERE Username = @Username";
            var profile = await _sqlConnection.QueryFirstOrDefaultAsync<Profile>(query, new { Username = participantUsernames });

            if (profile == null)
            {
                throw new ProfileNotFoundException($"A recipient with username {username} was not found");
            }

            participants.Add(profile);
        }

        return new UserConversation(conversationId, participants, conversation.ModifiedUnixTime, username);



    }

    public async Task UpdateConversationLastMessageTime(UserConversation senderConversation, long lastMessageTime)
    {
        var conversationId = senderConversation.ConversationId;
        var query = "UPDATE Conversations SET ModifiedUnixTime = @lastMessageTime WHERE ConversationId = @ConversationId";

        try
        {
            await _sqlConnection.ExecuteAsync(query, new { lastMessageTime, ConversationId = conversationId });
        }
        catch (SqlException ex)
        {
            if(ex.Number == 2627)
            {
                throw new ConversationNotFoundException($"A conversation with id {senderConversation.ConversationId} does not exist");
            }
            else
            {
                throw;
            }

        }
    }

    public async Task CreateUserConversation(UserConversation userConversation)
    {
        var queryConversationTable = "INSERT INTO Conversations (ConversationId, ModifiedUnixTime) VALUES (@ConversationId, @ModifiedUnixTime)";
        var queryConversationParticipantsTable = "INSERT INTO ConversationParticipants (ConversationId, Username) VALUES (@ConversationId, @Username)";

        await using var transaction = _sqlConnection.BeginTransaction();
        try
        {
            await _sqlConnection.ExecuteAsync(queryConversationTable,
                new
                {
                    ConversationId = userConversation.ConversationId,
                    ModifiedUnixTime = userConversation.LastMessageTime
                }, transaction);
            await _sqlConnection.ExecuteAsync(queryConversationParticipantsTable,
                new { ConversationId = userConversation.ConversationId, Username = userConversation.Username },
                transaction);
            foreach (var recipient in userConversation.Recipients)
            {
                await _sqlConnection.ExecuteAsync(queryConversationParticipantsTable,
                    new { ConversationId = userConversation.ConversationId, Username = recipient.Username },
                    transaction);
            }

            transaction.Commit();
        }
        catch (SqlException ex)
        {
            if (ex.Number == 2627)
            {
                throw new ConversationAlreadyExistsException(
                    $"A conversation with id {userConversation.ConversationId} already exists");
            }
            else
            {
                transaction.Rollback();
                throw;
            }
        }
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