namespace ChatApplication.Web.Dtos;

public record GetMessagesResult(
    List<ConversationMessage> Messages,
    string? ContinuationToken
    );