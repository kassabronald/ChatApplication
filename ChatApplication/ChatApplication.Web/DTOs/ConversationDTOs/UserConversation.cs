namespace ChatApplication.Web.Dtos;

public record UserConversation
{
    public UserConversation(string conversationId, List<Profile> participants, long lastMessageTime, string username)
    {
        ConversationId = conversationId;
        Participants = participants;
        LastMessageTime = lastMessageTime;
        Username = username;
    }

    public string ConversationId { get; init; }
    public List<Profile> Participants { get; init; }
    public long LastMessageTime { get; set; }

    public string Username { get; init; }
}