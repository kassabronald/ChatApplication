namespace ChatApplication.Exceptions.ConversationParticipantsExceptions;

public class DuplicateParticipantException : Exception
{
    public DuplicateParticipantException(string message) : base(message) { }

}