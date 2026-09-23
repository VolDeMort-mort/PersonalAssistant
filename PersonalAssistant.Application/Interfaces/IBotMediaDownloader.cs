namespace PersonalAssistant.Application.Interfaces;

public interface IBotMediaDownloader
{
    /// <summary>
    /// Downloads file from telegram
    /// </summary>
    /// <returns>Local path to the files</returns>
    Task<string?> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default);
}
