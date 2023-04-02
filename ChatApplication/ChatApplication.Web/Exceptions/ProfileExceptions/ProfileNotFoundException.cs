using ChatApplication.Web.Dtos;

namespace ChatApplication.Exceptions;

public class ProfileNotFoundException : Exception
{
    public readonly string Username;
    public ProfileNotFoundException(string message) : base(message) { }
    
    public ProfileNotFoundException() { }

    public ProfileNotFoundException(string message, string username)
    {
        Username = username;
    }
}