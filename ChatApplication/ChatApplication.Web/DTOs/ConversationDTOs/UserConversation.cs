namespace ChatApplication.Web.Dtos;

public record UserConversation
{
    public UserConversation(string conversationId, List<Profile> recipients, long lastMessageTime, string username)
    {
        ConversationId = conversationId;
        Recipients = recipients;
        LastMessageTime = lastMessageTime;
        Username = username;
    }

    public string ConversationId { get; init; }
    public List<Profile> Recipients { get; init; }
    public long LastMessageTime { get; set; }

    public string Username { get; init; }
}