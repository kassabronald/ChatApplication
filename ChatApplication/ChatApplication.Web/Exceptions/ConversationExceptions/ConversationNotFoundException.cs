namespace ChatApplication.Exceptions;

public class ConversationNotFoundException : Exception
{
    
    public ConversationNotFoundException(string message) : base(message) { }
    
    public ConversationNotFoundException() { }
    
}