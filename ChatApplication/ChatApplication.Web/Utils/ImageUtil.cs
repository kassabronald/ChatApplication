using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Utils;

public record ImageUtil(
    [Required] byte[] ImageData,
    [Required] string ContentType);

   
