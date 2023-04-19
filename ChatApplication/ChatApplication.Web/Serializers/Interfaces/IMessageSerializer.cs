using ChatApplication.Web.Dtos;

namespace ChatApplication.Serializers.Implementations;

public interface IMessageSerializer
{
    string SerializeMessage(Message message);

    Message DeserializeMessage(string serializedMessage);
}