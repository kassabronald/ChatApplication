using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record Profile(
    [Required] string Username,
    [Required] string FirstName,
    [Required] string LastName,
    string ProfilePictureId="");