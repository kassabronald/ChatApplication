namespace ChatApplication.Web.Dtos;

public record Conversation
{
    public string conversationId { get; init; }
    public List<Profile> participants { get; init; }
    public long lastMessageTime { get; set; }

    public Conversation(string conversationId, List<Profile> participants, long lastMessageTime)
    {
        this.conversationId = conversationId;
        this.participants = participants;
        this.lastMessageTime = lastMessageTime;
    }
}