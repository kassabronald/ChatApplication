namespace ChatApplication.Web.Dtos;

public record MessagesAndToken(
    List<Message> messages,
    string? continuationToken);