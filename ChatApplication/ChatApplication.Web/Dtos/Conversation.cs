namespace ChatApplication.Web.Dtos;

public record Conversation
{
    public string conversationId { get; init; }
    public Profile[] participants { get; init; }
    public long lastMessageTime { get; set; }

    public Conversation(string conversationId, Profile[] participants, long lastMessageTime)
    {
        this.conversationId = conversationId;
        this.participants = participants;
        this.lastMessageTime = lastMessageTime;
    }
}