using ChatApplication.Web.Dtos;

namespace ChatApplication.Serializers.Implementations;

public class JsonMessageSerializer : IMessageSerializer
{
    public string SerializeMessage(Message message)
    {
        throw new NotImplementedException();
    }

    public Message DeserializeMessage(string serializedMessage)
    {
        throw new NotImplementedException();
    }
}