namespace ChatApplication.Web.Dtos;

public record ConversationsAndToken(
    List<Conversation> Conversations,
    string? continuationToken
)
{
    public List<ConversationMetaData> ToMetadata()
    {
        List<ConversationMetaData> metadata = new();
        foreach (var conversation in Conversations)
        {
            ConversationMetaData conversationMetaData = new(
                conversation.conversationId,
                conversation.lastMessageTime,
                conversation.participants
            );
            metadata.Add(conversationMetaData);
        }

        return metadata;
    }
    
}