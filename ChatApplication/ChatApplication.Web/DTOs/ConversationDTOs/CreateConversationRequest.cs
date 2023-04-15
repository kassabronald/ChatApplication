using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record StartConversationRequest(
    [Required] List<string> Participants,
    SendMessageRequest FirstMessage
);
    