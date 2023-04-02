namespace ChatApplication.Web.Dtos;

public record GetAllConversationsResponse(
    List<ConversationMetaData> Conversations,
    string NextUri
    );