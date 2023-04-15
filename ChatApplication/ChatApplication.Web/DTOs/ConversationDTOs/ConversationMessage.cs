using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record ConversationMessage(
    [Required] string SenderUsername,
    [Required] string Text,
    [Required] long UnixTime
);