namespace ChatApplication.Exceptions;

public class ProfileAlreadyExistsException : Exception
{

    public ProfileAlreadyExistsException(string message) : base(message) { }

    
    
    public ProfileAlreadyExistsException() { }
    
    
}