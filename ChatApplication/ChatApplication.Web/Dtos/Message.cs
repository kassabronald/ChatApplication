using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record Message(
    [Required]string messageId,
    [Required]string senderUsername,
    [Required]string messageContent,
    [Required]string conversationId
);