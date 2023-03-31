using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Storage.Entities;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.Azure.Cosmos;

namespace ChatApplication.Storage;

public class CosmosConversationStore : IConversationStore
{
    private readonly CosmosClient _cosmosClient;
    private Container ConversationContainer => _cosmosClient.GetDatabase("MainDatabase").GetContainer("Conversations");
    
    public CosmosConversationStore(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }
    
    public async Task<Conversation> GetConversation(string conversationId)
    {
        try
        {
            var conversation = await ConversationContainer.ReadItemAsync<ConversationEntity>(conversationId,
                new PartitionKey(conversationId) ,
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
            conversation.lastMessageTime = lastMessageTime;
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

    public async Task StartConversation(Conversation conversation)
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
                    $"Conversation with id :{conversation.conversationId} already exists");
            }
            throw;
        }        

    }

    public async Task DeleteConversation(Conversation conversation)
    {
        try
        {
            await ConversationContainer.DeleteItemAsync<Conversation>(
                id: conversation.conversationId,
                partitionKey: new PartitionKey(conversation.conversationId)
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

    private ConversationEntity toEntity(Conversation conversation)
    {
        return new ConversationEntity
        {
            partitionKey = conversation.conversationId,
            id = conversation.conversationId,
            Participants = conversation.participants,
            lastMessageTime = conversation.lastMessageTime
        };
    }

    private Conversation ToConversation(ConversationEntity conversationEntity)
    {
        return new Conversation(
            conversationEntity.id,
            conversationEntity.Participants,
            conversationEntity.lastMessageTime
        );
    }
}