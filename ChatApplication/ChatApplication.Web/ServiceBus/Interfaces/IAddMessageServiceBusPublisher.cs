using ChatApplication.Web.Dtos;

namespace ChatApplication.ServiceBus.Interfaces;

public interface IAddMessageServiceBusPublisher
{
    /// <summary>
    /// Publish message to service bus.
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Task</returns>
    public Task Send(Message message);
}