using ChatApplication.Web.Dtos;

namespace ChatApplication.Serializers.Implementations;

public interface IMessageSerializer
{
    /// <param name="message"></param>
    /// <returns>string</returns>
    string SerializeMessage(Message message);
    
    /// <param name="serializedMessage"></param>
    /// <returns>Message</returns>
    Message DeserializeMessage(string serializedMessage);
}