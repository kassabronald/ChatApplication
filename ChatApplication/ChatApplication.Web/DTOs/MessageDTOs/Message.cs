using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record Message(
    [Required] string MessageId,
    [Required] string SenderUsername,
    [Required] string ConversationId,
    [Required] string Text,
    [Required] long CreatedUnixTime
    
);