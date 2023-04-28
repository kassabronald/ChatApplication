namespace ChatApplication.Web.Dtos;

public record GetMessagesParameters(string ConversationId, int Limit = 50,
    string ContinuationToken = "", long LastSeenMessageTime = 0);