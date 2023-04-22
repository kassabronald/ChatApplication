using ChatApplication.Web.Dtos;

namespace ChatApplication.ServiceBus.Interfaces;

public interface IStartConversationServiceBusPublisher
{
    public Task Send(StartConversationParameters parameters);
}