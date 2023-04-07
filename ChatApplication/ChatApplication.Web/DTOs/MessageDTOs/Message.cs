using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record Message(
    [Required] string MessageId,
    [Required] string SenderUsername,
    [Required] string Text,
    [Required] long CreatedUnixTime,
    [Required] string ConversationId
);