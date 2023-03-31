using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public interface IConversationService
{
    public Task AddMessage(Message message);

    public Task<ConversationMessageAndToken> GetConversationMessages(string conversationId, int limit, string continuationToken, long lastMessageTime);

    public Task<string> StartConversation(string messageId, string senderUsername, string messageContent,
        long createdTime, List<string> participants);
    
    public Task<ConversationsMetaDataAndToken> GetAllConversations(string username, int limit, string continuationToken);

}