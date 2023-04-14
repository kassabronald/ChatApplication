using ChatApplication.Utils;

namespace ChatApplication.Web.Dtos;

public record StartConversationResponse(string Id, long CreatedUnixTime, List<string> Participants);