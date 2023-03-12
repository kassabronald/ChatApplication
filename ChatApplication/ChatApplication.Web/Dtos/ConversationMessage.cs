using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record ConversationMessage(
    [Required]string senderUsername,
    [Required]string messageContent,
    [Required]long UnixTime
);