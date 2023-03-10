namespace ChatApplication.Web.Dtos;

public record Conversation(
    string conversationId,
    Profile[] participants,
    long lastMessageTime);