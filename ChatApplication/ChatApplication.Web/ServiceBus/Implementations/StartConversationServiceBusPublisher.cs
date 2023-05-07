using Azure.Messaging.ServiceBus;
using ChatApplication.Configuration;
using ChatApplication.Serializers.Implementations;
using ChatApplication.ServiceBus.Interfaces;
using ChatApplication.Web.Dtos;
using Microsoft.Extensions.Options;

namespace ChatApplication.ServiceBus;

public class StartConversationServiceBusPublisher : IStartConversationServiceBusPublisher
{
    private readonly ServiceBusSender _sender;
    private readonly IStartConversationParametersSerializer _serializer;
    
    public StartConversationServiceBusPublisher(ServiceBusClient serviceBusClient, IStartConversationParametersSerializer profileSerializer,
        IOptions<ServiceBusSettings> options
    )
    {
        _sender = serviceBusClient.CreateSender(options.Value.StartConversationQueueName);
        _serializer = profileSerializer;
    }
    
    public async Task Send(StartConversationParameters startConversationParameters)
    {
        var serializedParameters = _serializer.SerializeStartConversationParametersSerializer(startConversationParameters);
        await _sender.SendMessageAsync(new ServiceBusMessage(serializedParameters));
    }
}