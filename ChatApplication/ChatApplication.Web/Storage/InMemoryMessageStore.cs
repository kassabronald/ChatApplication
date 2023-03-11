using ChatApplication.Exceptions;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public class InMemoryMessageStore : IMessageStore
{
    private readonly Dictionary<string, KeyValuePair<Message, UnixTime>> _messages = new();
    public Task<UnixTime> AddMessage(Message message)
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
        return Task.FromResult(time);
    }
}