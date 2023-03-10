using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record MessageRequest(
    [Required]string messageId,
    [Required]string senderUsername,
    [Required]string messageContent
);