namespace ChatApplication.Exceptions;

public class ImageNotFoundException : Exception
{

    private string id="";
    public ImageNotFoundException(string message) : base(message) { }
    
    public ImageNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    
    public ImageNotFoundException() { }
    
    public ImageNotFoundException(string message, string id) : base(message)
    {
        this.id = id;
    }
}