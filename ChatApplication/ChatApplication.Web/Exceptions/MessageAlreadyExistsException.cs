namespace ChatApplication.Exceptions;

public class MessageAlreadyExistsException : Exception
{
    //make the exception so that it can be used to throw a message with a specific id
    private string _id = "";
    public MessageAlreadyExistsException(string message, string id) : base(message)
    {
        this._id = id;
    }

    public MessageAlreadyExistsException(string message)
    {
        //TODO: IMPLEMENT
        
    }
}