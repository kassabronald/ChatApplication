namespace ChatApplication.Web.Dtos;

public record StartConversationParameters(
    string messageId, string senderUsername, string messageContent,
    long createdTime, List<string> participants
    );