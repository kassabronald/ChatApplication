using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Utils;

public record ImageUtil(
    [Required] byte[] _imageData,
    [Required] string _contentType);

   
