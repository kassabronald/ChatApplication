using ChatApplication.Utils;

namespace ChatApplication.Web.Dtos;

public record StartConversationResponse(string ConversationId, long CreatedUnixTime);