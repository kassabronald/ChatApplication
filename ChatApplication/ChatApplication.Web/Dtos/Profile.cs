using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record Profile(
    [Required] string username, 
    [Required] string firstName, 
    [Required] string lastName,
    [Required] string ProfilePictureId);