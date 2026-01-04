using ChanSentry.CLI.Services;
using ChanSentry.CLI.Services.Interfaces;
using ChanSentry.Cli.Utils;
using ChanSentry.Common.Models;
using Spectre.Console;

namespace ChanSentry.CLI.Handlers;

public class DownloadHandler
{
    private readonly IWatchedThreadService _watchedThreadService;
    private readonly IThreadProcessingService _threadProcessingService;

    public DownloadHandler(IWatchedThreadService watchedThreadService, IThreadProcessingService threadProcessingService)
    {
        _watchedThreadService = watchedThreadService;
        _threadProcessingService = threadProcessingService;
    }

    public DownloadHandler()
        : this(new WatchedThreadService(), new ThreadProcessingService())
    {
    }

    public async Task StartAsync()
    {
        var watchedThreads = _watchedThreadService.ReadWatchedThreads();

        if (!HasWatchedThreads(watchedThreads))
        {
            DisplayNoThreadsMessage();
            return;
        }

        var running = true;
        while (running)
        {
            DisplayHeader();

            try
            {
                running = await ProcessThreadsLoopAsync(watchedThreads!);
            }
            catch (Exception ex)
            {
                DisplayError(ex);
                return;
            }
        }

        FinalizeAndExit(watchedThreads);
    }

    private async Task<bool> ProcessThreadsLoopAsync(List<WatchedThread> watchedThreads)
    {
        var shouldContinue = await ProcessAllThreadsAsync(watchedThreads);
        
        if (!shouldContinue)
            return false;
        
        watchedThreads = _watchedThreadService.RemoveFailedThreads(watchedThreads);
        _watchedThreadService.SaveWatchedThreads(watchedThreads);

        return !CheckExitKeys() && await CountdownWithExitCheckAsync(10);
    }

    private async Task<bool> ProcessAllThreadsAsync(List<WatchedThread> watchedThreads)
    {
        var activeThreads = watchedThreads.Where(t => t.ErrorCount < 3).ToList();

        for (int i = 0; i < activeThreads.Count; i++)
        {
            var thread = activeThreads[i];
            
            await _threadProcessingService.ProcessThreadAsync(thread, watchedThreads);
            _watchedThreadService.SaveWatchedThreads(watchedThreads);

            if (i < activeThreads.Count - 1)
            {
                if (!await CountdownWithExitCheckAsync(2))
                    return false;
            }
        }
        
        return true;
    }

    private static bool HasWatchedThreads(List<WatchedThread>? watchedThreads)
    {
        return watchedThreads != null && watchedThreads.Count > 0;
    }

    private static void DisplayHeader()
    {
        AnsiConsole.Clear();
        CliPrint.PrintTitle();
        AnsiConsole.MarkupLine("[bold cyan]Downloader Running...[/]");
        AnsiConsole.MarkupLine("[dim]Press ESC or Q to return to main menu[/]\n");
    }

    private static void DisplayNoThreadsMessage()
    {
        AnsiConsole.MarkupLine("[yellow]No threads found in watched-threads.json[/]");  
        AnsiConsole.MarkupLine("[dim]Press any key to return to main menu...[/]");
        Console.ReadKey(true);
    }

    private static void DisplayError(Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error reading watched threads: {ex.Message}[/]");
        AnsiConsole.MarkupLine("[dim]Press any key to return to main menu...[/]");
        Console.ReadKey(true);
    }

    private void FinalizeAndExit(List<WatchedThread>? watchedThreads)
    {
        if (watchedThreads is not null)
        {
            AnsiConsole.MarkupLine("\n[dim]Updating Watched Threads list...[/]");
            _watchedThreadService.SaveWatchedThreads(watchedThreads);
        }

        AnsiConsole.MarkupLine("[dim]Returning to main menu...[/]");
        System.Threading.Thread.Sleep(500);
    }

    private static async Task<bool> CountdownWithExitCheckAsync(int seconds)
    {
        for (int i = seconds * 100; i > 0; i--)
        {
            DisplayCountdown((int)Math.Ceiling(i / 100.0), seconds);
            await Task.Delay(10);

            if (CheckExitKeys())
            {
                AnsiConsole.WriteLine();
                return false;
            }
        }
        
        AnsiConsole.WriteLine();
        return true;
    }

    private static void DisplayCountdown(int remaining, int total)
    {
        var pluralSuffix = remaining != 1 ? "s" : "";
        AnsiConsole.Markup($"\r[dim]Next check in {remaining.ToString()} second{pluralSuffix}... (Press ESC or Q to exit)[/]  ");
    }

    private static bool CheckExitKeys()
    {
        if (!Console.KeyAvailable)
            return false;

        var key = Console.ReadKey(true);
        return key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q;
    }
}