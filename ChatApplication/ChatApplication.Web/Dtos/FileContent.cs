using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record FileContent(
    [Required] byte [] Data,
    [Required] string ContentType
    );
