using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record StartConversationRequest
{
    [Required] public List<string> Participants { get; init; }
    [Required] public SendMessageRequest FirstSendMessage { get; init; }
    
    public StartConversationRequest(List<string> participants, SendMessageRequest firstSendMessage)
    {
        Participants = participants;
        FirstSendMessage = firstSendMessage;
    }
}