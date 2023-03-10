using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record MessageResponse(
    [Required] long CreatedUnixTime
    );