namespace ChatApplication.Web.Dtos;

public record GetConversationsResponse(
    List<ConversationMetaData> Conversations,
    string NextUri
    );