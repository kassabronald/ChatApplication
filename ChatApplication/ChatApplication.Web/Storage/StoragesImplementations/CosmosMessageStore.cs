using System.Net;
using System.Text.Json;
using ChatApplication.Exceptions;
using ChatApplication.Storage.Entities;
using ChatApplication.Utils;
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
        
        //var response = await query.ToFeedIterator().ReadNextAsync();
        //var messages = response.Select(toMessage).ToList();
        //string newContinuationToken = response.ContinuationToken;
        //Console.WriteLine("helo");
        //Console.WriteLine(newContinuationToken);
        //return new MessagesAndToken(messages, newContinuationToken);
    
        //return new MessagesAndToken(messages, JsonSerializer.Deserialize<JsonElement>(response.ContinuationToken)[0].GetProperty("token").GetString());
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
    
    public async Task<ConversationMessageAndToken> GetConversationMessages(string conversationId, int limit, string continuationToken, long lastMessageTime)
    {
        var messages = GetConversationMessagesUtil(conversationId, limit, continuationToken, lastMessageTime);
        var conversationMessages = new List<ConversationMessage>();
        foreach (var message in messages.Result.messages)
        {
            conversationMessages.Add(new ConversationMessage(message.senderUsername, message.messageContent, message.createdUnixTime));
        }
        return new ConversationMessageAndToken(conversationMessages, messages.Result.continuationToken);
    }
};