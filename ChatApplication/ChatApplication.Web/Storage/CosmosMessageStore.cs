using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Storage.Entities;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.Azure.Cosmos;

namespace ChatApplication.Storage;

public class CosmosMessageStore: IMessageStore
{
    
    private readonly CosmosClient _cosmosClient;
    
    public CosmosMessageStore(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }
    
    private Container MessageContainer => _cosmosClient.GetDatabase("MainDatabase").GetContainer("Messages");
    private Container ConversationContainer => _cosmosClient.GetDatabase("MainDatabase").GetContainer("Conversations");

    public async Task<UnixTime> AddMessage(Message message)
    {
        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
        try
        {
            var conversation = await ConversationContainer.ReadItemAsync<ConversationEntity>(message.conversationId,
                new PartitionKey(message.conversationId) ,
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session 
                });
            conversation.Resource.lastMessageTime = unixTime;
            await ConversationContainer.ReplaceItemAsync(conversation.Resource, message.conversationId, new PartitionKey(message.conversationId));
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                //TODO: IMPLEMENT
                throw new ConversationNotFoundException();
            }
            throw;
        }
        
        
        var entity = toEntity(message);
        try
        {
            await MessageContainer.CreateItemAsync(entity);
        }
        catch (Exception e)
        {
            if (e is CosmosException cosmosException && cosmosException.StatusCode == HttpStatusCode.Conflict)
            {
                throw new MessageAlreadyExistsException($"Message with id {message.messageId} already exists");
            }

            throw;
        }
        
        return new UnixTime(unixTime);
    }

    private MessageEntity toEntity(Message message)
    {
        return new MessageEntity(
            partitionKey: message.conversationId,
            id: message.messageId,
            SenderUsername: message.senderUsername,
            MessageContent: message.messageContent);
    }
};