using System.ComponentModel.DataAnnotations;
using ChatApplication.Utils;

namespace ChatApplication.Web.Dtos;

public record Message(
    [Required]string MessageId,
    [Required]string SenderUsername,
    [Required]string MessageContent,
    [Required]long CreatedUnixTime,
    [Required]string ConversationId
);