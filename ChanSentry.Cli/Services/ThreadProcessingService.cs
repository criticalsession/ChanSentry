using ChanSentry.Cli.Utils;
using ChanSentry.Common.Models;
using Spectre.Console;

namespace ChanSentry.CLI.Services;

public class ThreadProcessingService
{
    private readonly ThreadFetchService _fetchService;
    private readonly MediaDownloadService _downloadService;

    public ThreadProcessingService()
    {
        _fetchService = new ThreadFetchService();
        _downloadService = new MediaDownloadService();
    }

    public async Task ProcessThreadAsync(WatchedThread thread)
    {
        DisplayThreadCheckMessage(thread);
        
        var fetchResult = await _fetchService.FetchThreadAsync(thread);

        if (fetchResult.IsSuccess && fetchResult.ThreadData != null)
        {
            thread.LastChecked = DateTime.UtcNow;
            await ProcessSuccessfulFetchAsync(thread, fetchResult.ThreadData);
        }
        else if (fetchResult.IsNotModified)
        {
            thread.LastChecked = DateTime.UtcNow;
            DisplayNotModifiedMessage(thread);
        }
        else
        {
            HandleFetchError(thread, fetchResult.StatusCode);
        }
    }

    private async Task ProcessSuccessfulFetchAsync(WatchedThread thread, Common.Models.Thread threadData)
    {
        UpdateThreadSubjectIfNeeded(thread, threadData);
        
        var (mediaPosts, newMedia) = GetMediaPosts(thread, threadData);
        
        await _downloadService.DownloadMediaFilesAsync(newMedia, thread);
        
        thread.TotalDownloadedFiles = mediaPosts.Count;
    }

    private static void UpdateThreadSubjectIfNeeded(WatchedThread thread, Common.Models.Thread threadData)
    {
        if (!string.IsNullOrWhiteSpace(thread.Subject))
            return;

        var subject = threadData.Posts.FirstOrDefault()?.Subject ?? string.Empty;
        thread.Subject = subject;
        AnsiConsole.MarkupLine($"[blue]Retrieved subject: {FileNameSanitizer.EscapeMarkup(subject)}[/]");
    }

    private static (List<Post> mediaPosts, List<Post> newMedia) GetMediaPosts(WatchedThread thread, Common.Models.Thread threadData)
    {
        AnsiConsole.MarkupLine($"[blue]Successfully fetched thread {thread.ThreadId} on /{thread.Board}/[/]");

        var mediaPosts = threadData.Posts.Where(post => post.HasMedia).ToList();
        var newMedia = mediaPosts.Skip(thread.TotalDownloadedFiles).ToList();
        
        AnsiConsole.MarkupLine($"[green]Found {newMedia.Count} new media files in thread {thread.ThreadId}[/]");
        
        return (mediaPosts, newMedia);
    }

    private static void DisplayThreadCheckMessage(WatchedThread thread)
    {
        var displaySubject = string.IsNullOrWhiteSpace(thread.Subject) ? "No Subject" : thread.Subject;
        var escapedSubject = FileNameSanitizer.EscapeMarkup(displaySubject);
        AnsiConsole.MarkupLine($"[grey]Checking: [/]{thread.Board}/{thread.ThreadId} - [green]{escapedSubject}[/]");
    }

    private static void DisplayNotModifiedMessage(WatchedThread thread)
    {
        AnsiConsole.MarkupLine($"[blue]Thread {thread.ThreadId} on /{thread.Board}/ has not been modified since last check.[/]");
    }

    private static void HandleFetchError(WatchedThread thread, System.Net.HttpStatusCode? statusCode)
    {
        thread.ErrorCount++;
        
        AnsiConsole.MarkupLine($"[red]Failed to fetch thread {thread.ThreadId} on /{thread.Board}/ - Status Code: {statusCode} (Total Errors: {thread.ErrorCount}/3)[/]");

        if (thread.ErrorCount >= 3)
        {
            AnsiConsole.MarkupLine($"[red]Thread has failed {thread.ErrorCount} times and will be deleted from Watched Threads list.[/]");
        }
    }
}
