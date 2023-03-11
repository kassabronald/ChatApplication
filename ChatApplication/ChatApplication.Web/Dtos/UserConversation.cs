using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record UserConversation(
    [Required] string username,
    [Required] string conversationId
);