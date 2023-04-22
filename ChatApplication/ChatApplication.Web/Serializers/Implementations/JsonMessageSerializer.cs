using ChatApplication.Web.Dtos;
using Newtonsoft.Json;

namespace ChatApplication.Serializers.Implementations;

public class JsonMessageSerializer : IMessageSerializer
{
    public string SerializeMessage(Message message)
    {
        return JsonConvert.SerializeObject(message);
    }

    public Message DeserializeMessage(string serializedMessage)
    {
        return JsonConvert.DeserializeObject<Message> (serializedMessage);
    }
}