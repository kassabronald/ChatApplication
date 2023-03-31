namespace ChatApplication.Web.Dtos;

public record Conversation
{
    public string conversationId { get; init; }
    public List<Profile> participants { get; init; }
    public long lastMessageTime { get; set; }
    
    public string username { get; init; }

    public Conversation(string conversationId, List<Profile> participants, long lastMessageTime, string username)
    {
        this.conversationId = conversationId;
        this.participants = participants;
        this.lastMessageTime = lastMessageTime;
        this.username = username;
    }
}