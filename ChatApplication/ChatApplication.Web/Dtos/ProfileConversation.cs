using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record ProfileConversation(
    [Required] string username,
    [Required] string conversationId
);