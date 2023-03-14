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
    public async Task AddMessage(Message message)
    {
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
    }
    public async Task DeleteMessage(Message message)
    {
        try
        {
            await MessageContainer.DeleteItemAsync<Message>(
                id: message.messageId,
                partitionKey: new PartitionKey(message.conversationId)
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
    public async Task<List<Message> > GetConversationMessages(string conversationId)
    {
        var query = MessageContainer.GetItemQueryIterator<MessageEntity>(
            new QueryDefinition("SELECT * FROM Messages WHERE Messages.partitionKey = @partitionKey ORDER BY Messages.CreatedUnixTime DESC")
                .WithParameter("@partitionKey", conversationId));
        var messages = new List<Message>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            foreach (var entity in response)
            {
                messages.Add(toMessage(entity));
            }
        }
        return messages;
    }
    private Message toMessage(MessageEntity entity)
    {
        return new Message(
            messageId: entity.id,
            senderUsername: entity.SenderUsername,
            messageContent: entity.MessageContent,
            createdUnixTime: entity.CreatedUnixTime,
            conversationId: entity.partitionKey);
    }
    
    private MessageEntity toEntity(Message message)
    {
        return new MessageEntity(
            partitionKey: message.conversationId,
            id: message.messageId,
            SenderUsername: message.senderUsername,
            CreatedUnixTime:message.createdUnixTime,
            MessageContent: message.messageContent);
    }
};