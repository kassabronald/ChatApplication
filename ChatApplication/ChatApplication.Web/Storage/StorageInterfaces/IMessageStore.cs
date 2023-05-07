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
    
    /// <summary>
    /// Get all messages in a conversation
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>Task with GetMessagesResult</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage cannot be reached <br></br></throws>
    Task<GetMessagesResult> GetMessages(GetMessagesParameters parameters);

    /// <summary>
    /// Delete a message
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Task</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage cannot be reached <br></br></throws>
    Task DeleteMessage(Message message);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="conversationId"></param>
    /// <param name="messageId"></param>
    /// <returns>Task with a Message</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage cannot be reached <br></br></throws>
    /// <throws><b>MessageNotFoundException</b> is thrown if message is not found <br></br></throws>
    Task<Message> GetMessage(string conversationId, string messageId);
}