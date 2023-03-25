using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public interface IMessageStore
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <throws><b>ConversationNotFoundException </b> if conversationId is not found<br></br> <br></br></throws>
    /// <throws><b>MessageAlreadyExistsException</b> if messageId already exists</throws>
    Task AddMessage(Message message);
    
    Task<List<Message> > GetConversationMessagesUtil(string conversationId);
    
    Task<List<ConversationMessage> > GetConversationMessages(string conversationId);
    
    Task DeleteMessage(Message message);
    
}