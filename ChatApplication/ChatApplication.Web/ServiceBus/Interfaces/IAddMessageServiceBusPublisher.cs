using ChatApplication.Web.Dtos;

namespace ChatApplication.ServiceBus.Interfaces;

public interface IAddMessageServiceBusPublisher
{
    public Task Send(Message message);
}