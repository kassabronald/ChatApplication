using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public interface IConversationService
{
    public Task AddMessage(Message message);

    public Task<List<Message> > GetConversationMessages(string conversationId);

    public Task<string> StartConversation(string messageId, string senderUsername, string messageContent,
        long createdTime, List<string> participants);

}