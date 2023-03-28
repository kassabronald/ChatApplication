namespace ChatApplication.Web.Dtos;

public record GetConversationMessagesResponse(
string NextUri,
List<ConversationMessage> Messages
);