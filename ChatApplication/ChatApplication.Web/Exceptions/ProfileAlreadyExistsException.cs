namespace ChatApplication.Exceptions;

public class ProfileAlreadyExistsException : Exception
{
    private string profile = "";
    public ProfileAlreadyExistsException(string message) : base(message) { }

    public ProfileAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }
    
    public ProfileAlreadyExistsException() { }
    
    public ProfileAlreadyExistsException(string message, string profile) : base(message)
    {
        this.profile = profile;
    }
}