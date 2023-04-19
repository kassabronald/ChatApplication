using ChatApplication.ServiceBus.Interfaces;
using ChatApplication.Web.Dtos;
using Azure.Messaging.ServiceBus;


namespace ChatApplication.ServiceBus;

public class AddMessageServiceBusPublisher : IAddMessageServiceBusPublisher
{
    private readonly ServiceBusSender _sender;
    public Task Send(Message message)
    {
        throw new NotImplementedException();
    }
}