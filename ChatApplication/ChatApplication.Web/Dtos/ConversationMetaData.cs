namespace ChatApplication.Web.Dtos;

public record ConversationMetaData(
    string id,
    long lastModifiedUnixTime,
    List<Profile> Recipients
    );