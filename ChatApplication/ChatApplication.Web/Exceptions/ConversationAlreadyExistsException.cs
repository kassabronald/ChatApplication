namespace ChatApplication.Exceptions;

public class ConversationAlreadyExistsException:Exception
{
    public ConversationAlreadyExistsException(string message) : base(message){}

    public ConversationAlreadyExistsException() { }
}