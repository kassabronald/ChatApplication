namespace ChatApplication.Web.Dtos;

public record StartConversationRequest
{
    List<string> Participants { get; init; }
    MessageRequest FirstMessage { get; init; }
}