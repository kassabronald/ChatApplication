using ChatApplication.Web.Dtos;
using Newtonsoft.Json;

namespace ChatApplication.Serializers.Implementations;

public class JsonStartConversationParametersSerializer : IStartConversationParametersSerializer
{
    public string SerializeStartConversationParametersSerializer(StartConversationParameters parameters)
    {
        return JsonConvert.SerializeObject(parameters);
    }

    public StartConversationParameters DeserializeStartConversationParameters(string serializedStartConversationParameters)
    {
        return JsonConvert.DeserializeObject<StartConversationParameters>(serializedStartConversationParameters);
    }
}