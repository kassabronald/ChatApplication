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


    private Conversation ToConversation(ConversationEntity conversationEntity)
    {
        return new Conversation(
            conversationEntity.id,
            conversationEntity.Participants,
            conversationEntity.lastMessageTime
        );
    }

    public async Task ChangeConversationLastMessageTime(Conversation conversation, long lastMessageTime)
    {
        try
        {
            conversation.lastMessageTime = lastMessageTime;
            await ConversationContainer.ReplaceItemAsync(conversation, conversation.conversationId,
                new PartitionKey(conversation.conversationId));
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

    private MessageEntity toEntity(Message message)
    {
        return new MessageEntity(
            partitionKey: message.conversationId,
            id: message.messageId,
            SenderUsername: message.senderUsername,
            MessageContent: message.messageContent);
    }
    
    public async Task<Message[]> GetConversationMessages(string conversationId)
    {
        //get elements with same partition key from MessageContainer
        var query = MessageContainer.GetItemQueryIterator<MessageEntity>(
            new QueryDefinition("SELECT * FROM Conversaions WHERE Conversations.partitionKey = @partitionKey")
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

        return messages.ToArray();
    }
    private Message toMessage(MessageEntity entity)
    {
        return new Message(
            messageId: entity.id,
            senderUsername: entity.SenderUsername,
            messageContent: entity.MessageContent,
            conversationId: entity.partitionKey);
    }
    
    
};