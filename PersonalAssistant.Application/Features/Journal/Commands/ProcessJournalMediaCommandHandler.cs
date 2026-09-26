using MediatR;
using Microsoft.Extensions.Logging;
using PersonalAssistant.Application.Features.Journal.Events;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Features.Journal.Commands;

public class ProcessJournalMediaCommandHandler : IRequestHandler<ProcessJournalMediaCommand>
{
    private readonly IJournalRepository _repository;
    private readonly IBotMediaDownloader _downloader;
    private readonly IPublisher _publisher;
    private readonly ILogger<ProcessJournalMediaCommandHandler> _logger;

    public ProcessJournalMediaCommandHandler(
        IJournalRepository repository,
        IBotMediaDownloader downloader,
        IPublisher publisher,
        ILogger<ProcessJournalMediaCommandHandler> logger)
    {
        _repository = repository;
        _downloader = downloader;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(ProcessJournalMediaCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.EntryId, cancellationToken);

        if (entry is null || string.IsNullOrEmpty(entry.TelegramFileId))
        {
            _logger.LogWarning("Journal entry {EntryId} not found or has no TelegramFileId.", request.EntryId);
            return;
        }

        if (entry.Status == ProcessingStatus.Pending)
        {
            var localPath = await _downloader.DownloadFileAsync(entry.TelegramFileId, cancellationToken);

            if (localPath is null)
            {
                _logger.LogError("Failed to download media for journal entry {EntryId}.", entry.Id);
                return;
            }

            entry.MarkDownloaded(localPath);
            await _repository.UpdateAsync(entry, cancellationToken);
            _logger.LogInformation("Media for journal entry {EntryId} downloaded.", entry.Id);

            // TODO(B6): publish after successful transcription, not after download
            await _publisher.Publish(new JournalEntryStored(entry.Id, entry.ChatId, entry.MessageId), cancellationToken);
        }

        // TODO: transcription -> entry.MarkTranscribed(text)
    }
}
