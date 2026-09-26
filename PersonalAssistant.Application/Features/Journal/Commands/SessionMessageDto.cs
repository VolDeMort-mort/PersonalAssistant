namespace PersonalAssistant.Application.Features.Journal.Commands;

public record SessionMessageDto(DtoMessageType Type, string? Text, string? FileId, DateTime CreatedAt, int MessageId);

