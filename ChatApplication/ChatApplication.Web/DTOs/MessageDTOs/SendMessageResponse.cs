using System.ComponentModel.DataAnnotations;

namespace ChatApplication.Web.Dtos;

public record SendMessageResponse(
    [Required] long CreatedUnixTime
);