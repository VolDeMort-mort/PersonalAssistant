using PersonalAssistant.Application.Features.Journal.Commands;
using PersonalAssistant.Application.Features.Journal.Queries;

using MediatR;
using Telegram.Bot.Types;


namespace PersonalAssistant.Presentation.Bot.Handlers;

public class JournalMessageHandler : IMessageHandler
{
    private readonly ISender _sender;
    public JournalMessageHandler(ISender sender)
    {
        _sender = sender;
    }
    public async Task<bool> CanHandleAsync(MessageContext context, CancellationToken ct) =>
    await _sender.Send(new IsJournalRecordingQuery(context.ChatId), ct);

    public async Task HandleAsync(MessageContext context, CancellationToken ct)
    {
        var dto = ToDto(context.Message);
        if (dto is not null)
            await _sender.Send(new AddJournalSessionMessageCommand(context.ChatId, dto), ct);
    }

    private static SessionMessageDto? ToDto(Message m) => m switch
    {
        { Text: { Length: > 0 } text } => new SessionMessageDto(DtoMessageType.Text, text, null, m.Date, m.MessageId),
        { Voice: { } voice } => new SessionMessageDto(DtoMessageType.Voice, null, voice.FileId, m.Date, m.MessageId),
        { VideoNote: { } note } => new SessionMessageDto(DtoMessageType.Video, null, note.FileId, m.Date, m.MessageId),
        _ => null
    };


}

