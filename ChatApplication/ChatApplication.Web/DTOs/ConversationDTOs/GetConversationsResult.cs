namespace ChatApplication.Web.Dtos;

public record GetConversationsResult(
    List<UserConversation> Conversations,
    string? ContinuationToken
)
{
    public List<ConversationMetaData> ToMetadata()
    {
        List<ConversationMetaData> metadata = new();
        foreach (var conversation in Conversations)
        {
            ConversationMetaData conversationMetaData = new(
                conversation.ConversationId,
                conversation.LastMessageTime,
                conversation.Participants
            );
            metadata.Add(conversationMetaData);
        }

        return metadata;
    }
    
}