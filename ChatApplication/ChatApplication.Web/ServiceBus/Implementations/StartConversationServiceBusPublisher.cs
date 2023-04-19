using Azure.Messaging.ServiceBus;
using ChatApplication.Configuration;
using ChatApplication.Serializers.Implementations;
using ChatApplication.Web.Dtos;
using Microsoft.Extensions.Options;

namespace ChatApplication.ServiceBus;

public class StartConversationServiceBusPublisher
{
    private readonly ServiceBusSender _sender;
    private readonly IStartConversationParametersSerializer _serializer;
    
    public StartConversationServiceBusPublisher(ServiceBusSender sender, IStartConversationParametersSerializer serializer,
        IOptions<ServiceBusSettings> options
    )
    {
        _sender = sender;
        _serializer = serializer;
    }
    
    public async Task Send(StartConversationParameters parameters)
    {
        var serializedParameters = _serializer.SerializeStartConversationParametersSerializer(parameters);
        await _sender.SendMessageAsync(new ServiceBusMessage(serializedParameters));
    }
    
    
}