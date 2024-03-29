namespace ChatApplication.Storage.Entities;

public record MessageEntity(
    string partitionKey,
    string id,
    string SenderUsername,
    long CreatedUnixTime,
    string MessageContent
    );