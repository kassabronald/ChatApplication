using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public interface IConversationStore
{

    public Task<Conversation> GetConversation(string conversationId);
    public Task ChangeConversationLastMessageTime(Conversation conversation, long lastMessageTime);
}