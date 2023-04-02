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
    
    public async Task<Conversation> GetConversation(string username, string conversationId)
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
                throw new ConversationNotFoundException();
            }

            throw;
        }
    }

    public async Task ChangeConversationLastMessageTime(Conversation conversation, long lastMessageTime)
    {
        try
        {
            conversation.LastMessageTime = lastMessageTime;
            var entity = toEntity(conversation);
            await ConversationContainer.ReplaceItemAsync<ConversationEntity>(entity,entity.id,
                new PartitionKey(entity.partitionKey));
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ConversationNotFoundException();
            }

            throw;
        }
    }

    public async Task CreateConversation(Conversation conversation)
    {
        var entity = toEntity(conversation);
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
                    $"Conversation with id :{conversation.ConversationId} already exists");
            }
            throw;
        }        

    }

    public async Task DeleteConversation(Conversation conversation)
    {
        try
        {
            await ConversationContainer.DeleteItemAsync<Conversation>(
                id: conversation.ConversationId,
                partitionKey: new PartitionKey(conversation.Username)
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

    public async Task<ConversationAndToken> GetAllConversations(string username, int limit, string continuationToken, long lastConversationTime)
    {
        return await GetAllConversationsUtil(username, limit, continuationToken, lastConversationTime);
    }

    public async Task<ConversationAndToken> GetAllConversationsUtil(string username, int limit, string continuationToken, long lastConversationTime)
    {
        //TODO: get continuation token and return it, also use limit

        QueryRequestOptions options = new QueryRequestOptions();
        options.MaxItemCount = Int32.Min(Int32.Max(limit,1), 100);
        var query = ConversationContainer.GetItemLinqQueryable<ConversationEntity>(true, string.IsNullOrEmpty(continuationToken) ? null : continuationToken, options)
            .Where(m => m.partitionKey == username && m.lastMessageTime > lastConversationTime)
            .OrderByDescending(m => m.lastMessageTime);

        using (FeedIterator<ConversationEntity> iterator = query.ToFeedIterator())
        {
            FeedResponse<ConversationEntity> response = await iterator.ReadNextAsync();
            var receivedConversations = response.Select(ToConversation).ToList();
            string newContinuationToken = response.ContinuationToken;
            return new ConversationAndToken(receivedConversations, newContinuationToken);
        }
    }

    private ConversationEntity toEntity(Conversation conversation)
    {
        return new ConversationEntity
        {
            partitionKey = conversation.Username,
            id = conversation.ConversationId,
            Participants = conversation.Participants,
            lastMessageTime = conversation.LastMessageTime,
        };
    }

    private Conversation ToConversation(ConversationEntity conversationEntity)
    {
        return new Conversation(
            conversationEntity.id,
            conversationEntity.Participants,
            conversationEntity.lastMessageTime,
            conversationEntity.partitionKey
        );
    }
}