using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record SendMessageRequest(
    [Required]string Id,
    [Required]string SenderUsername,
    [Required]string Text
);