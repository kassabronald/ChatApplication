using ChatApplication.Web.Dtos;

namespace ChatApplication.Serializers.Implementations;

public interface IStartConversationParametersSerializer
{
    string SerializeStartConversationParametersSerializer(StartConversationParameters parameters);

    StartConversationParameters DeserializeStartConversationParameters(string serializedStartConversationParameters);
}