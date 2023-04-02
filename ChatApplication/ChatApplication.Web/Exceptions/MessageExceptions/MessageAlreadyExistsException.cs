namespace ChatApplication.Exceptions;

public class MessageAlreadyExistsException : Exception
{


    public MessageAlreadyExistsException(string message) : base(message) { }

    public MessageAlreadyExistsException() { }

}