namespace ChatApplication.Web.Dtos;

public record ConversationMessageAndToken(
    List<ConversationMessage> Messages,
    string? ContinuationToken
    );