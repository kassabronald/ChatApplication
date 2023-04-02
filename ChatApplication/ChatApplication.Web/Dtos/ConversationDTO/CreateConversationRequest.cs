using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record StartConversationRequest
{
    [Required] public List<string> Participants { get; init; }
    [Required] public MessageRequest FirstMessage { get; init; }
    
    public StartConversationRequest(List<string> participants, MessageRequest firstMessage)
    {
        Participants = participants;
        FirstMessage = firstMessage;
    }
}