namespace PersonalAssistant.Application.Interfaces;

public interface IAudioTranscriptionService
{
    Task<string?> TranscribeAudioAsync(string filePath, CancellationToken cancellationToken = default);
}
