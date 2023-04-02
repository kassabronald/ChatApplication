using System.ComponentModel.DataAnnotations;
using ChatApplication.Utils;

namespace ChatApplication.Web.Dtos;

public record Message(
    [Required]string messageId,
    [Required]string senderUsername,
    [Required]string messageContent,
    [Required]long createdUnixTime,
    [Required]string conversationId
);