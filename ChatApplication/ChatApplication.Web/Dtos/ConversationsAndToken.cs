namespace ChatApplication.Web.Dtos;

public record ConversationsMetaDataAndToken(
    List<Conversation> Conversations,
    string continuationToken
    );