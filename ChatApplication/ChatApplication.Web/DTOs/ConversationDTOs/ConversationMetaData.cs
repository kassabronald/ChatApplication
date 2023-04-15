namespace ChatApplication.Web.Dtos;

public record ConversationMetaData(
    string Id,
    long LastModifiedUnixTime,
    Profile Recipient
);