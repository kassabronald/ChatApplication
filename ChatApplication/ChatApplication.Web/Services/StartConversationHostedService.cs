using Azure.Messaging.ServiceBus;
using ChatApplication.Configuration;
using ChatApplication.Serializers.Implementations;
using Microsoft.Extensions.Options;

namespace ChatApplication.Services;

public class StartConversationHostedService : IHostedService
{
    private readonly IConversationService _conversationService;
    private readonly IStartConversationParametersSerializer _startConversationSerializer;
    private readonly ServiceBusProcessor _startConversationServiceBusProcessor;
    
    public StartConversationHostedService(IConversationService conversationService, IStartConversationParametersSerializer startConversationSerializer, ServiceBusClient serviceBusClient, IOptions<ServiceBusSettings> options)
    {
        _conversationService = conversationService;
        _startConversationSerializer = startConversationSerializer;
        _startConversationServiceBusProcessor = serviceBusClient.CreateProcessor(options.Value.StartConversationQueueName);
        
        // add handler to process messages
        _startConversationServiceBusProcessor.ProcessMessageAsync += MessageHandler;

        // add handler to process any errors
        _startConversationServiceBusProcessor.ProcessErrorAsync += ErrorHandler;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _startConversationServiceBusProcessor.StartProcessingAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _startConversationServiceBusProcessor.StopProcessingAsync(cancellationToken);
    }
    
    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string data = args.Message.Body.ToString();

        var startConversationParameters = _startConversationSerializer.DeserializeStartConversationParameters(data);
        await _conversationService.StartConversation(startConversationParameters);

        await args.CompleteMessageAsync(args.Message);
    }
    
    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }
}