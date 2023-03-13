namespace ChatApplication.Exceptions;

public class ProfileNotFoundException : Exception
{
    
    public string MissingProfileUsername { get; set; }

    public ProfileNotFoundException(string message, string missingProfileUsername) : base(message)
    {
        MissingProfileUsername = missingProfileUsername;
    }

    
    
    public ProfileNotFoundException() { }
    
}