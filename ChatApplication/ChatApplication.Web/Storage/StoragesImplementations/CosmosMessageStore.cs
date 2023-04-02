using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Storage.Entities;
using ChatApplication.Web.Dtos;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
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

    public async Task<MessagesAndToken> GetConversationMessagesUtil(string conversationId, int limit,
        string continuationToken, long lastMessageTime)
    {
        //TODO: get continuation token and return it, also use limit

        QueryRequestOptions options = new QueryRequestOptions();
        options.MaxItemCount = Int32.Min(Int32.Max(limit,1), 100);
        var query = MessageContainer.GetItemLinqQueryable<MessageEntity>(true, string.IsNullOrEmpty(continuationToken) ? null : continuationToken, options)
            .Where(m => m.partitionKey == conversationId && m.CreatedUnixTime > lastMessageTime)
            .OrderByDescending(m => m.CreatedUnixTime);

        using (FeedIterator<MessageEntity> iterator = query.ToFeedIterator())
        {
            FeedResponse<MessageEntity> response = await iterator.ReadNextAsync();
            var receivedMessages = response.Select(toMessage).ToList();
            string newContinuationToken = response.ContinuationToken;
            return new MessagesAndToken(receivedMessages, newContinuationToken);
        }
    }
    private Message toMessage(MessageEntity entity)
    {
        return new Message(
            MessageId: entity.id,
            SenderUsername: entity.SenderUsername,
            MessageContent: entity.MessageContent,
            CreatedUnixTime: entity.CreatedUnixTime,
            ConversationId: entity.partitionKey);
    }
    
    private MessageEntity toEntity(Message message)
    {
        return new MessageEntity(
            partitionKey: message.ConversationId,
            id: message.MessageId,
            SenderUsername: message.SenderUsername,
            CreatedUnixTime:message.CreatedUnixTime,
            MessageContent: message.MessageContent);
    }
    
    public async Task<ConversationMessageAndToken> GetConversationMessages(string conversationId, int limit, string continuationToken, long lastMessageTime)
    {
        var messages = await GetConversationMessagesUtil(conversationId, limit, continuationToken, lastMessageTime);
        var conversationMessages = new List<ConversationMessage>();
        foreach (var message in messages.Messages)
        {
            conversationMessages.Add(new ConversationMessage(message.SenderUsername, message.MessageContent, message.CreatedUnixTime));
        }
        return new ConversationMessageAndToken(conversationMessages, messages.ContinuationToken);
    }
};