using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Exceptions.StorageExceptions;
using ChatApplication.Storage.Entities;
using ChatApplication.Web.Dtos;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace ChatApplication.Storage;

public class CosmosMessageStore : IMessageStore
{
    private readonly CosmosClient _cosmosClient;

    public CosmosMessageStore(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }

    private Container MessageContainer => _cosmosClient.GetDatabase("MainDatabase").GetContainer("Messages");

    public async Task AddMessage(Message message)
    {
        var entity = ToEntity(message);
        try
        {
            await MessageContainer.CreateItemAsync(entity);
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.Conflict)
            {
                throw new MessageAlreadyExistsException($"Message with id {message.MessageId} already exists");
            }

            if (e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not add message with id {message.MessageId}", e);
            }

            throw;
        }
    }

    public async Task DeleteMessage(Message message)
    {
        try
        {
            await MessageContainer.DeleteItemAsync<Message>(
                id: message.MessageId,
                partitionKey: new PartitionKey(message.ConversationId)
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
                throw new StorageUnavailableException($"Could not delete message with id {message.MessageId}", e);
            }

            throw;
        }
    }

    public async Task<Message> GetMessage(string conversationId, string messageId)
    {
        try
        {
            var message = await MessageContainer.ReadItemAsync<MessageEntity>(messageId,
                new PartitionKey(conversationId),
            new ItemRequestOptions
            {
                ConsistencyLevel = ConsistencyLevel.Session
            });
            return ToMessage(message.Resource);
        }
        catch(CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                throw new MessageNotFoundException($"A message with id {messageId} does not exist");
            }
            
            if (e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not get message with id {messageId}", e);
            }

            throw;
        }
    }

    public async Task<GetMessagesResult> GetMessages(GetMessagesParameters parameters)
    {
        var options = new QueryRequestOptions
        {
            MaxItemCount = int.Min(int.Max(parameters.Limit, 1), 100)
        };

        try
        {
            var query = MessageContainer.GetItemLinqQueryable<MessageEntity>(true,
                    string.IsNullOrEmpty(parameters.ContinuationToken) ? null : parameters.ContinuationToken, options)
                .Where(m => m.partitionKey == parameters.ConversationId &&
                            m.CreatedUnixTime > parameters.LastSeenMessageTime)
                .OrderByDescending(m => m.CreatedUnixTime);

            using var iterator = query.ToFeedIterator();
            var response = await iterator.ReadNextAsync();
            var receivedMessages = response.Select(ToMessage).ToList();
            var newContinuationToken = response.ContinuationToken;
            var conversationMessages = receivedMessages.Select(message =>
                    new ConversationMessage(message.SenderUsername, message.Text, message.CreatedUnixTime))
                .ToList();
            return new GetMessagesResult(conversationMessages, newContinuationToken);
        }
        catch (CosmosException e)
        {
            if (e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not get messages for conversation with id {parameters.ConversationId}", e);
            }

            throw;
        }
    }

    private static Message ToMessage(MessageEntity entity)
    {
        return new Message(
            MessageId: entity.id,
            SenderUsername: entity.SenderUsername,
            Text: entity.MessageContent,
            CreatedUnixTime: entity.CreatedUnixTime,
            ConversationId: entity.partitionKey);
    }

    private static MessageEntity ToEntity(Message message)
    {
        return new MessageEntity(
            partitionKey: message.ConversationId,
            id: message.MessageId,
            SenderUsername: message.SenderUsername,
            CreatedUnixTime: message.CreatedUnixTime,
            MessageContent: message.Text);
    }
};