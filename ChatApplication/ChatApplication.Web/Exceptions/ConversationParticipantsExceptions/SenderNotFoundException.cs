namespace ChatApplication.Exceptions.ConversationParticipantsExceptions;

public class SenderNotFoundException : Exception
{
    public SenderNotFoundException(string message) : base(message) { }
    
}