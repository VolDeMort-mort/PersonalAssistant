namespace PersonalAssistant.Application.Interfaces;

public interface IAudioTranscripService
{
    Task<string?> TranscribeAudioAsync(string filePath, CancellationToken cancellationToken = default);
}
