using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record MessageRequest(
    [Required]string MessageId,
    [Required]string SenderUsername,
    [Required]string MessageContent
);