using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public interface IConversationService
{
    Task<UnixTime> AddMessage(Message message);
}