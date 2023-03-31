namespace ChatApplication.Web.Dtos;

public record ConversationMessageAndToken(
    List<ConversationMessage> messages,
    string continuationToken
    );