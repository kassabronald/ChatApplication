using Azure.Messaging.ServiceBus;
using ChatApplication.Configuration;
using ChatApplication.Serializers.Implementations;
using Microsoft.Extensions.Options;

namespace ChatApplication.Services;

public class AddMessageServiceBusPublisher : IHostedService
{
    
    private readonly IMessageSerializer _messageSerializer;
    private readonly IConversationService _conversationService;
    private readonly ServiceBusProcessor _addMessageProcessor;
    
    public AddMessageServiceBusPublisher(ServiceBusClient serviceBusClient, IMessageSerializer messageSerializer, IConversationService conversationService, IOptions<ServiceBusSettings> options)
    {
        _messageSerializer = messageSerializer;
        _conversationService = conversationService;
        _addMessageProcessor = serviceBusClient.CreateProcessor(options.Value.AddMessageQueueName);
        
        _addMessageProcessor.ProcessMessageAsync += MessageHandler;
        _addMessageProcessor.ProcessErrorAsync += ErrorHandler;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _addMessageProcessor.StartProcessingAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _addMessageProcessor.StopProcessingAsync(cancellationToken);
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string data = args.Message.Body.ToString();
        Console.WriteLine($"Received: {data}");
        
        var message = _messageSerializer.DeserializeMessage(data);
        await _conversationService.AddMessage(message);
        
        await args.CompleteMessageAsync(args.Message);
    } 
    
    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }
}