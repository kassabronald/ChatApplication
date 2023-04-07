using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Utils;

public record Image(
    [Required] byte[] ImageData,
    [Required] string ContentType);

   
