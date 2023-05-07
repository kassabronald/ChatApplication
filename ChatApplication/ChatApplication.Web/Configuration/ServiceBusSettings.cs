namespace ChatApplication.Configuration;

public class ServiceBusSettings
{
    public string ConnectionString { get; set; }
    public string AddMessageQueueName { get; set; }
    public string StartConversationQueueName { get; set; }
}