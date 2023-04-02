namespace ChatApplication.Web.Dtos;

public record Conversation
{
    public string ConversationId { get; init; }
    public List<Profile> Participants { get; init; }
    public long LastMessageTime { get; set; }
    
    public string Username { get; init; }

    public Conversation(string conversationId, List<Profile> participants, long lastMessageTime, string username)
    {
        this.ConversationId = conversationId;
        this.Participants = participants;
        this.LastMessageTime = lastMessageTime;
        this.Username = username;
    }
}