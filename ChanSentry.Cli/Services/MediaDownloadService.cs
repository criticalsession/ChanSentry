using ChanSentry.CLI.Services.Interfaces;
using ChanSentry.Cli.Utils;
using ChanSentry.Common.Models;
using Spectre.Console;

namespace ChanSentry.CLI.Services;

public class MediaDownloadService : IMediaDownloadService
{
    private const string UserAgent = "ChanSentry/1.0";
    private const int DelayBetweenDownloadsMs = 100;

    public async Task DownloadMediaFilesAsync(List<Post> posts, WatchedThread thread)
    {
        var downloadPath = PrepareDownloadDirectory(thread);
        
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);

        foreach (var post in posts)
        {
            await DownloadSingleFileAsync(httpClient, post, thread.Board, downloadPath);
        }
    }

    private static string PrepareDownloadDirectory(WatchedThread thread)
    {
        var folderName = GetDownloadFolderName(thread);
        var downloadPath = Path.Combine("downloads", thread.Board, folderName);
        
        RenameOldFolderIfNeeded(thread, downloadPath);
        Directory.CreateDirectory(downloadPath);
        
        return downloadPath;
    }

    private static void RenameOldFolderIfNeeded(WatchedThread thread, string newPath)
    {
        var oldPath = Path.Combine("downloads", thread.Board, thread.ThreadId.ToString());
        
        if (!Directory.Exists(oldPath) || oldPath == newPath)
            return;

        try
        {
            Directory.Move(oldPath, newPath);
            AnsiConsole.MarkupLine("[yellow]Renamed folder to include subject[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to rename folder: {ex.Message}[/]");
        }
    }

    private static string GetDownloadFolderName(WatchedThread thread)
    {
        var sanitizedSubject = FileNameSanitizer.Sanitize(thread.Subject);
        
        if (string.IsNullOrWhiteSpace(sanitizedSubject))
        {
            return thread.ThreadId.ToString();
        }
        
        return $"{thread.ThreadId} - {sanitizedSubject}";
    }

    private static async Task DownloadSingleFileAsync(HttpClient httpClient, Post post, string boardCode, string downloadPath)
    {
        try
        {
            var fileUrl = post.GetFileUrl(boardCode);
            if (string.IsNullOrEmpty(fileUrl))
                return;

            var fileName = BuildFileName(post);
            var filePath = Path.Combine(downloadPath, fileName);

            if (File.Exists(filePath))
            {
                LogFileExists(fileName);
                return;
            }

            await DownloadAndSaveFileAsync(httpClient, fileUrl, filePath, fileName);
            await Task.Delay(DelayBetweenDownloadsMs);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]> Error downloading file: {ex.Message}[/]");
        }
    }

    private static string BuildFileName(Post post)
    {
        var sanitizedFileName = FileNameSanitizer.Sanitize(post.FileName);
        var prefix = !string.IsNullOrEmpty(sanitizedFileName) ? $"{sanitizedFileName} - " : "";
        return $"{prefix}{post.InternalFileIdentifier}{post.FileExtension}";
    }

    private static async Task DownloadAndSaveFileAsync(HttpClient httpClient, string fileUrl, string filePath, string fileName)
    {
        var response = await httpClient.GetAsync(fileUrl);

        if (response.IsSuccessStatusCode)
        {
            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filePath, fileBytes);
            LogFileDownloaded(fileName);
        }
        else
        {
            LogDownloadFailed(fileName, response.StatusCode);
        }
    }

    private static void LogFileExists(string fileName)
    {
        var displayFileName = FileNameSanitizer.EscapeMarkup(fileName);
        AnsiConsole.MarkupLine($"[grey]> File {displayFileName} already exists[/]");
    }

    private static void LogFileDownloaded(string fileName)
    {
        var displayFileName = FileNameSanitizer.EscapeMarkup(fileName);
        AnsiConsole.MarkupLine($"[green]> Downloaded {displayFileName}[/]");
    }

    private static void LogDownloadFailed(string fileName, System.Net.HttpStatusCode statusCode)
    {
        var displayFileName = FileNameSanitizer.EscapeMarkup(fileName);
        AnsiConsole.MarkupLine($"[red]> Failed to download {displayFileName} - Status: {statusCode}[/]");
    }
}
