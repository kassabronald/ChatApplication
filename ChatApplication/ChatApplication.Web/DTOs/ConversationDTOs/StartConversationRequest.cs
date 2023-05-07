using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record StartConversationRequest(
    [Required] List<string> Participants,
    SendMessageRequest FirstMessage
)
{
    public bool IsValid(out string? error)
    {
        if (Participants.Count < 2)
        {
            error = "Participants must be at least 2";
            return false;
        }
        
        if (!FirstMessage.IsValid(out error))
        {
            return false;
        }

        error = null;
        return true;
    }
}