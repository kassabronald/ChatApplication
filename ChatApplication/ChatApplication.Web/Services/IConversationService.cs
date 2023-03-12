using ChatApplication.Utils;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public interface IConversationService
{
    public Task AddMessage(Message message);
    
    

}