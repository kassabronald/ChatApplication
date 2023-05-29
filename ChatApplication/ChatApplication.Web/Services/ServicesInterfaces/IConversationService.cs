using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public interface IConversationService
{
    /// <summary>
    /// Add Message
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Task</returns>
    /// <throws><b>ConversationNotFoundException</b> is thrown if conversation does not exist <br></br></throws>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage layer cannot be reached<br></br></throws>
    public Task AddMessage(Message message);
    
    /// <summary>
    /// Get all messages in a conversation
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>Task with GetMessagesResult</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage layer cannot be reached<br></br></throws>
    public Task<GetMessagesResult> GetMessages(GetMessagesParameters parameters);
    
    /// <summary>
    /// Start a conversation
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>Task with a string</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage layer cannot be reached<br></br></throws>
    public Task<string> StartConversation(StartConversationParameters parameters);
    
    /// <summary>
    /// Get All conversations of a User
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>Task with GetConversationsResult</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage layer cannot be reached<br></br></throws>
    public Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters);
    
    /// <summary>
    /// Enqueue message to the message bus
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Task</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage layer cannot be reached<br></br></throws>
    /// <throws><b>MessageAlreadyExistsException</b> is thrown if message already exists<br></br></throws>
    /// <throws><b>ConversationNotFoundException</b> is thrown if conversation does not exist <br></br></throws>
    public Task EnqueueAddMessage(Message message);
    
    /// <summary>
    /// Enqueue start conversation to the message bus
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>Task with string</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage layer cannot be reached<br></br></throws>
    /// <throws><b>ConversationAlreadyExistsException</b> is thrown if conversation already exists<br></br></throws>
    /// <throws><b>ProfileNotFoundException</b> is thrown if profile does not exist <br></br></throws>
    /// <throws><b>SenderNotFoundException</b> is thrown if sender does not exist <br></br></throws>
    /// <throws><b>DuplicateParticipantException</b> is thrown if participant already exists <br></br></throws>
    public Task<string> EnqueueStartConversation(StartConversationParameters parameters);

}