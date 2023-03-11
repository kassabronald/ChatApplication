using ChatApplication.Exceptions;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public class InMemoryMessageStore : IMessageStore
{
    private readonly Dictionary<string, KeyValuePair<Message, UnixTime>> _messages = new();
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
        
        UnixTime time= new(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        _messages[message.messageId] = new(message, time);
    }
    
    
    //may be wrong implementation
    public Task<Message[]> GetConversationMessages(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException($"Invalid conversation id {conversationId}", nameof(conversationId));
        }
        return Task.FromResult(_messages.Values
            .Where(pair => pair.Key.conversationId == conversationId)
            .Select(pair => pair.Key)
            .ToArray());
    }

    public Task<Conversation> GetConversation(string conversationId)
    {
        throw new NotImplementedException();
    }

    public Task ChangeConversationLastMessageTime(Conversation conversation, long lastMessageTime)
    {
        throw new NotImplementedException();
    }
}