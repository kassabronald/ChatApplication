namespace ChatApplication.Web.Dtos;

public record MessagesAndToken(
    List<Message> Messages,
    string? ContinuationToken);