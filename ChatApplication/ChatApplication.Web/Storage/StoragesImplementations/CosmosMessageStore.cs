using System.Net;
using ChatApplication.Exceptions;
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
        var entity = toEntity(message);
        try
        {
            await MessageContainer.CreateItemAsync(entity);
        }
        catch (Exception e)
        {
            if (e is CosmosException cosmosException && cosmosException.StatusCode == HttpStatusCode.Conflict)
            {
                throw new MessageAlreadyExistsException($"Message with id {message.MessageId} already exists");
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

            throw;
        }
    }

    public async Task<GetMessagesResult> GetMessages(GetMessagesParameters parameters)
    {
        var options = new QueryRequestOptions
        {
            MaxItemCount = int.Min(int.Max(parameters.Limit, 1), 100)
        };
        var query = MessageContainer.GetItemLinqQueryable<MessageEntity>(true,
                string.IsNullOrEmpty(parameters.ContinuationToken) ? null : parameters.ContinuationToken, options)
            .Where(m => m.partitionKey == parameters.ConversationId && m.CreatedUnixTime > parameters.LastSeenMessageTime)
            .OrderByDescending(m => m.CreatedUnixTime);

        using var iterator = query.ToFeedIterator();
        var response = await iterator.ReadNextAsync();
        var receivedMessages = response.Select(toMessage).ToList();
        var newContinuationToken = response.ContinuationToken;
        var conversationMessages = receivedMessages.Select(message =>
                new ConversationMessage(message.SenderUsername, message.Text, message.CreatedUnixTime))
            .ToList();
        return new GetMessagesResult(conversationMessages, newContinuationToken);
    }

    private Message toMessage(MessageEntity entity)
    {
        return new Message(
            MessageId: entity.id,
            SenderUsername: entity.SenderUsername,
            Text: entity.MessageContent,
            CreatedUnixTime: entity.CreatedUnixTime,
            ConversationId: entity.partitionKey);
    }

    private MessageEntity toEntity(Message message)
    {
        return new MessageEntity(
            partitionKey: message.ConversationId,
            id: message.MessageId,
            SenderUsername: message.SenderUsername,
            CreatedUnixTime: message.CreatedUnixTime,
            MessageContent: message.Text);
    }
};