using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage.Entities;

public record ConversationEntity
{
    public string partitionKey { get; init;}
    public string id { get; init;}
    public List<Profile> Participants { get; set; }
    public long lastMessageTime { get; set; }
    
}
    