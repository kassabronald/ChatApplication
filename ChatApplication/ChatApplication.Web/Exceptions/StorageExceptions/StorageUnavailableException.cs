namespace ChatApplication.Exceptions.StorageExceptions;

public class StorageUnavailableException: Exception
{
    public StorageUnavailableException(string message) : base(message)
    {
    }
    public StorageUnavailableException(string message, Exception e) : base(message, e)
    {
    }
    
}