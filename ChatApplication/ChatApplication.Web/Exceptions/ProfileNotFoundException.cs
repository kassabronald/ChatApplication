namespace ChatApplication.Exceptions;

public class ProfileNotFoundException : Exception
{
    public ProfileNotFoundException(string message) : base(message) { }
    
    public ProfileNotFoundException() { }
    
}