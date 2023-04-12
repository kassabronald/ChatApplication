namespace ChatApplication.Web.Dtos;

public record GetConversationsParameters(string Username, int Limit = 50, string ContinuationToken = "", long LastSeenConversationTime=0);