using ChatApplication.Web.Dtos;

namespace ChatApplication.Serializers.Implementations;

public interface IStartConversationParametersSerializer
{
    /// <summary>
    /// Serialize StartConversationParameters.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>string</returns>
    string SerializeStartConversationParametersSerializer(StartConversationParameters parameters);

    /// <summary>
    /// Deserialize StartConversationParameters.
    /// </summary>
    /// <param name="serializedStartConversationParameters"></param>
    /// <returns>StartConversationParameters</returns>
    StartConversationParameters DeserializeStartConversationParameters(string serializedStartConversationParameters);
}