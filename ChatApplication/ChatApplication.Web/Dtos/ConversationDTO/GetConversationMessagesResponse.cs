namespace ChatApplication.Web.Dtos;

public record GetConversationMessagesResponse(
    List<ConversationMessage> Messages,
    string NextUri
);