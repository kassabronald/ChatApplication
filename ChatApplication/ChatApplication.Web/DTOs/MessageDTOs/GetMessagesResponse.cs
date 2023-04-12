namespace ChatApplication.Web.Dtos;

public record GetMessagesResponse(
    List<ConversationMessage> Messages,
    string NextUri
);