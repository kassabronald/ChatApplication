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
    /// <throws>ChatApplication.Exceptions.ConversationNotFoundException</throws>
    /// <throws>ChatApplication.Exceptions.MessageAlreadyExistsException</throws>
    Task<UnixTime> AddMessage(Message message);
}