using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public interface IConversationStore
{

    /// <param name="username"></param>
    /// <param name="conversationId"></param>
    /// <returns>Task with UserConversation </returns>
    /// <throws><b>ConversationNotFoundException</b> is thrown if conversation does not exist <br></br></throws>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage layer cannot be reached<br></br></throws>
    public Task<UserConversation> GetUserConversation(string username, string conversationId);
    
    /// <param name="senderConversation"></param>
    /// <param name="lastMessageTime"></param>
    /// <returns>Task</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage cannot be reached <br></br></throws>
    public Task UpdateConversationLastMessageTime(UserConversation senderConversation, long lastMessageTime);
    
    /// <param name="userConversation"></param>
    /// <returns>Task</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage cannot be reached <br></br></throws>
    /// <throws><b>ConversationAlreadyExistsException</b> is thrown if conversation already exists <br></br></throws>
    public Task CreateUserConversation(UserConversation userConversation);

    /// <summary>
    /// Deletes a User Conversation
    /// </summary>
    /// <param name="userConversation"></param>
    /// <returns>Task</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage cannot be reached <br></br></throws>
    public Task DeleteUserConversation(UserConversation userConversation);
    
    /// <summary>
    /// Get all of a User's conversations
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>Task with GetConversationsResult</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage cannot be reached <br></br></throws>
    public Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters);
}