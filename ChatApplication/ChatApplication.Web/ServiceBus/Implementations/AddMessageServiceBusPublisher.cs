using ChatApplication.ServiceBus.Interfaces;
using ChatApplication.Web.Dtos;
using Azure.Messaging.ServiceBus;
using ChatApplication.Configuration;
using ChatApplication.Serializers.Implementations;
using Microsoft.Extensions.Options;

namespace ChatApplication.ServiceBus;

public class AddMessageServiceBusPublisher : IAddMessageServiceBusPublisher
{
    private readonly ServiceBusSender _sender;
    private readonly IMessageSerializer _messageSerializer;
    
    public AddMessageServiceBusPublisher(ServiceBusClient serviceBusClient, IMessageSerializer messageSerializer, IOptions<ServiceBusSettings> options)
    {
        _sender = serviceBusClient.CreateSender(options.Value.AddMessageQueueName);
        _messageSerializer = messageSerializer;
    }
    
    public Task Send(Message message)
    {
        var serializedMessage = _messageSerializer.SerializeMessage(message);
        return _sender.SendMessageAsync(new ServiceBusMessage(serializedMessage));
    }
}