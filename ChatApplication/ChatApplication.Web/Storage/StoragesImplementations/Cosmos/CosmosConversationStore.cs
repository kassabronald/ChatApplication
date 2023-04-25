using System.Net;
using ChatApplication.Exceptions;
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

            throw;
        }
    }
    
    private async Task UpdateConversationUserLastMessageTime(UserConversation userConversation, long lastMessageTime)
    {
        userConversation.LastMessageTime = lastMessageTime;
        var entity = toEntity(userConversation);
        await ConversationContainer.ReplaceItemAsync<ConversationEntity>(entity, entity.id,
            new PartitionKey(entity.partitionKey));
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
        var entity = toEntity(userConversation);
        try
        {
            await ConversationContainer.CreateItemAsync(entity);
        }
        catch (CosmosException e)
        {
            //TODO: No issues if conflict
            if (e.StatusCode == HttpStatusCode.Conflict)
            {
                throw new ConversationAlreadyExistsException(
                    $"Conversation with id :{userConversation.ConversationId} already exists");
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
            throw;
        }
    }

    public async Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters)
    {
        var options = new QueryRequestOptions
        {
            MaxItemCount = int.Min(int.Max(parameters.Limit,1), 100)
        };
        var query = ConversationContainer.GetItemLinqQueryable<ConversationEntity>(true, string.IsNullOrEmpty(parameters.ContinuationToken) ? null : parameters.ContinuationToken, options)
            .Where(m => m.partitionKey == parameters.Username && m.lastMessageTime > parameters.LastSeenConversationTime)
            .OrderByDescending(m => m.lastMessageTime);

        using var iterator = query.ToFeedIterator();
        var response = await iterator.ReadNextAsync();
        var receivedConversations = response.Select(ToConversation).ToList();
        var newContinuationToken = response.ContinuationToken;
        return new GetConversationsResult(receivedConversations, newContinuationToken);
    }
    

    private ConversationEntity toEntity(UserConversation userConversation)
    {
        return new ConversationEntity
        {
            partitionKey = userConversation.Username,
            id = userConversation.ConversationId,
            Participants = userConversation.Recipients,
            lastMessageTime = userConversation.LastMessageTime,
        };
    }

    private UserConversation ToConversation(ConversationEntity conversationEntity)
    {
        return new UserConversation(
            conversationEntity.id,
            conversationEntity.Participants,
            conversationEntity.lastMessageTime,
            conversationEntity.partitionKey
        );
    }
}