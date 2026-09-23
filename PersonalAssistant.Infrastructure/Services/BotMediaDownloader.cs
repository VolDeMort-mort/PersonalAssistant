using PersonalAssistant.Application.Interfaces;
using Telegram.Bot;

namespace PersonalAssistant.Infrastructure.Services;


public class BotMediaDownloader: IBotMediaDownloader
{
    private readonly ITelegramBotClient _botClient;
    private readonly string _downloadFolder;

    public BotMediaDownloader(ITelegramBotClient botClient)
    {
        _botClient = botClient;

        _downloadFolder = Path.Combine(Directory.GetCurrentDirectory(), "DownloadedMedia");

        if (!Directory.Exists(_downloadFolder))
        {
            Directory.CreateDirectory(_downloadFolder);
        }
    }

    public async Task<string?> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var file = await _botClient.GetFile(fileId, cancellationToken);
            if (string.IsNullOrEmpty(file.FilePath)) return null;

            var extension = Path.GetExtension(file.FilePath);
            var localFileName = $"{Guid.NewGuid()}{extension}";
            var localFilePath = Path.Combine(_downloadFolder, localFileName);

            await using var fileStream = new FileStream(localFilePath, FileMode.Create);
            await _botClient.DownloadFile(file.FilePath, fileStream, cancellationToken);

            return localFilePath;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
