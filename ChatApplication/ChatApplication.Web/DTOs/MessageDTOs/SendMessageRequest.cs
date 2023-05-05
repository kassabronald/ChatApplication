using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record SendMessageRequest(
    [Required] string Id,
    [Required] string SenderUsername,
    [Required] string Text
)
{
    public bool IsValid(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            error = "Id must not be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SenderUsername))
        {
            error = "SenderUsername must not be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Text))
        {
            error = "Text must not be empty";
            return false;
        }

        error = null;
        return true;
    }
}
