namespace ChatApplication.Web.Dtos;

public record GetConversationsResult(
    List<UserConversation> Conversations,
    string? ContinuationToken
)
{
    public List<ConversationMetaData> ToMetadata(string senderUsername)
    {
        List<ConversationMetaData> metadata = new();
        foreach (var conversation in Conversations)
        {
            var recipient = new Profile("", "", "", "");
            foreach (var participant in conversation.Recipients.Where(participant => participant.Username != senderUsername))
            {
                recipient = participant;
                break;
            }
            ConversationMetaData conversationMetaData = new(
                conversation.ConversationId,
                conversation.LastMessageTime,
                recipient
            );
            metadata.Add(conversationMetaData);
        }

        return metadata;
    }
    
}