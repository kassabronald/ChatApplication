using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Exceptions.StorageExceptions;
using ChatApplication.Storage.Entities;
using ChatApplication.Web.Dtos;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace ChatApplication.Storage;

public class CosmosConversationStore : IConversationStore
{
    private readonly CosmosClient _cosmosClient;
    private Container ConversationContainer => _cosmosClient.GetDatabase("MainDatabase").GetContainer("Conversations");
    
    public CosmosConversationStore(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }
    
    public async Task<UserConversation> GetUserConversation(string username, string conversationId)
    {
        try
        {
            var conversation = await ConversationContainer.ReadItemAsync<ConversationEntity>(conversationId,
                new PartitionKey(username) ,
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session 
                });
            return ToConversation(conversation.Resource);
        }
        catch(CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ConversationNotFoundException($"Could not resolve conversation with id : {conversationId}");
            }

            if (e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not get conversation with id : {conversationId} for user {username}", e);
            }
            
            throw;
        }
    }
    
    private async Task UpdateConversationUserLastMessageTime(UserConversation userConversation, long lastMessageTime)
    {
        userConversation.LastMessageTime = lastMessageTime;
        var entity = ToEntity(userConversation);

        try
        {
            await ConversationContainer.ReplaceItemAsync<ConversationEntity>(entity, entity.id,
                new PartitionKey(entity.partitionKey));
        }
        catch (CosmosException e)
        {
            if (e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not update conversation with id {userConversation.ConversationId}'s last message time", e);
            }
            
            throw;
        }
    }
    
    public async Task UpdateConversationLastMessageTime(UserConversation senderConversation, long lastMessageTime)
    {
        var participantsUsernames = new List<string> {senderConversation.Username};
        var conversationId = senderConversation.ConversationId;
        participantsUsernames.AddRange(senderConversation.Recipients.Select(x => x.Username));
        
        var participantsConversation = await Task.WhenAll(participantsUsernames.Select(username =>
            GetUserConversation(username, conversationId)));
        
        await Task.WhenAll(participantsConversation.Select(conversation=> UpdateConversationUserLastMessageTime(conversation, lastMessageTime)));
    }

    public async Task CreateUserConversation(UserConversation userConversation)
    {
        var entity = ToEntity(userConversation);
        
        try
        {
            await ConversationContainer.CreateItemAsync(entity);
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.Conflict)
            {
                throw new ConversationAlreadyExistsException(
                    $"Conversation with id :{userConversation.ConversationId} already exists");
            }
            if (e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not create conversation with id : {userConversation.ConversationId} for user {userConversation.Username}", e);
            }
            
            throw;
        }        

    }

    public async Task DeleteUserConversation(UserConversation userConversation)
    {
        try
        {
            await ConversationContainer.DeleteItemAsync<UserConversation>(
                id: userConversation.ConversationId,
                partitionKey: new PartitionKey(userConversation.Username)
            );
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }
            if (e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not delete conversation with id : {userConversation.ConversationId} for user {userConversation.Username}", e);
            }
            
            throw;
        }
    }

    public async Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters)
    {
        var options = new QueryRequestOptions
        {
            MaxItemCount = int.Min(int.Max(parameters.Limit,1), 100)
        };

        try
        {
            var query = GetFilteredConversations(parameters.Username, parameters.LastSeenConversationTime, parameters.ContinuationToken, options);
            
            using var iterator = query.ToFeedIterator();
            var response = await iterator.ReadNextAsync();
            var receivedConversations = response.Select(ToConversation).ToList();
            var newContinuationToken = response.ContinuationToken;
            return new GetConversationsResult(receivedConversations, newContinuationToken);
        }
        catch (CosmosException e)
        {
            if (e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not get conversations for user {parameters.Username}", e);
            }
            
            throw;
        }
    }
    

    private static ConversationEntity ToEntity(UserConversation userConversation)
    {
        return new ConversationEntity
        {
            partitionKey = userConversation.Username,
            id = userConversation.ConversationId,
            Participants = userConversation.Recipients,
            lastMessageTime = userConversation.LastMessageTime,
        };
    }

    private static UserConversation ToConversation(ConversationEntity conversationEntity)
    {
        return new UserConversation(
            conversationEntity.id,
            conversationEntity.Participants,
            conversationEntity.lastMessageTime,
            conversationEntity.partitionKey
        );
    }
    
    private IQueryable<ConversationEntity> GetFilteredConversations(string username, long lastSeenConversationTime, string? continuationToken, QueryRequestOptions options)
    {
        return ConversationContainer.GetItemLinqQueryable<ConversationEntity>(true, string.IsNullOrEmpty(continuationToken) ? null : continuationToken, options)
            .Where(m => m.partitionKey == username &&
                        m.lastMessageTime > lastSeenConversationTime)
            .OrderByDescending(m => m.lastMessageTime);
    }
}