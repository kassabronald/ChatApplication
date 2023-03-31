using ChatApplication.Exceptions;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public class InMemoryMessageStore : IMessageStore
{
    private readonly Dictionary<string, KeyValuePair<Message, long>> _messages = new();
    public async Task AddMessage(Message message)
    {
        if (message == null ||
            string.IsNullOrWhiteSpace(message.messageId) ||
            string.IsNullOrWhiteSpace(message.conversationId) ||
            string.IsNullOrWhiteSpace(message.senderUsername) ||
            string.IsNullOrWhiteSpace(message.messageContent)
        )
        {
            throw new ArgumentException($"Invalid message {message}", nameof(message));
        }
        if (_messages.ContainsKey(message.messageId))
        {
            throw new MessageAlreadyExistsException(message.messageId);
        }
        _messages[message.messageId] = new(message, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    public Task<MessagesAndToken> GetConversationMessagesUtil(string conversationId, int limit, string continuationToken, long lastMessageTime)
    {
        throw new NotImplementedException();
    }

    Task<ConversationMessageAndToken> IMessageStore.GetConversationMessages(string conversationId, int limit, string continuationToken, long lastMessageTime)
    {
        throw new NotImplementedException();
    }


    //may be wrong implementation
    public async Task<List<Message>> GetConversationMessages(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException($"Invalid conversation id {conversationId}", nameof(conversationId));
        }
        //return this in list form
        
        List<Message> messages = new();
        foreach (var message in _messages)
        {
            if (message.Value.Key.conversationId == conversationId)
            {
                messages.Add(message.Value.Key);
            }
        }

        return messages;
    }

    public Task DeleteMessage(Message message)
    {
        throw new NotImplementedException();
    }

    public Task<Conversation> GetConversation(string conversationId)
    {
        throw new NotImplementedException();
    }

    
    public Task ChangeConversationLastMessageTime(Conversation conversaiton, long time)

    {
        throw new NotImplementedException();
    }
}