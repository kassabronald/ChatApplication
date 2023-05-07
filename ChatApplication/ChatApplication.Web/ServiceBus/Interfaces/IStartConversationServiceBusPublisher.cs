using ChatApplication.Web.Dtos;

namespace ChatApplication.ServiceBus.Interfaces;

public interface IStartConversationServiceBusPublisher
{
    /// <summary>
    /// Publish StartConversationParameters to service bus.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>Task</returns>
    public Task Send(StartConversationParameters parameters);
}