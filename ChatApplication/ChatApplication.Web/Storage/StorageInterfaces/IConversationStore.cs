using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public interface IConversationStore
{

    public Task<Conversation> GetConversation(string username, string conversationId);
    public Task ChangeConversationLastMessageTime(Conversation conversation, long lastMessageTime);
    public Task CreateConversation(Conversation conversation);

    public Task DeleteConversation(Conversation conversation);
    public Task<ConversationAndToken> GetAllConversations(string username, int limit, string continuationToken, long lastConversationTime);
    
    Task<ConversationAndToken> GetAllConversationsUtil(string username, int limit, string continuationToken, long lastConversationTime);
    
}