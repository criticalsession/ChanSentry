using ChanSentry.CLI.Services;
using ChanSentry.CLI.Services.Interfaces;
using ChanSentry.Cli.Utils;
using ChanSentry.Common.Models;
using Spectre.Console;

namespace ChanSentry.CLI.Handlers;

public class ManageThreadsHandler
{
    private readonly IWatchedThreadService _watchedThreadService;
    private readonly IThreadFetchService _threadFetchService;

    public ManageThreadsHandler(IWatchedThreadService watchedThreadService, IThreadFetchService threadFetchService)
    {
        _watchedThreadService = watchedThreadService;
        _threadFetchService = threadFetchService;
    }

    public ManageThreadsHandler()
        : this(new WatchedThreadService(), new ThreadFetchService())
    {
    }

    public async Task AddThreadAsync()
    {
        AnsiConsole.Clear();
        CliPrint.PrintTitle();
        AnsiConsole.MarkupLine("[bold cyan]Add Watched Thread[/]\n");

        var board = PromptForBoard();
        if (string.IsNullOrWhiteSpace(board))
        {
            AnsiConsole.MarkupLine("[red]Board cannot be empty.[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to return...[/]");
            Console.ReadKey(true);
            return;
        }

        var threadId = PromptForThreadId();
        if (threadId <= 0)
        {
            AnsiConsole.MarkupLine("[red]Invalid thread ID.[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to return...[/]");
            Console.ReadKey(true);
            return;
        }

        var watchedThreads = _watchedThreadService.ReadWatchedThreads() ?? new List<WatchedThread>();

        if (ThreadAlreadyExists(watchedThreads, board, threadId))
        {
            AnsiConsole.MarkupLine($"[yellow]Thread {threadId} on /{board}/ is already being watched.[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to return...[/]");
            Console.ReadKey(true);
            return;
        }

        await FetchAndAddThreadAsync(watchedThreads, board, threadId);
    }

    private static string PromptForBoard()
    {
        return AnsiConsole.Ask<string>("[cyan]Enter the board code (e.g., g, pol, v):[/]")
            .Trim()
            .ToLowerInvariant();
    }

    private static long PromptForThreadId()
    {
        return AnsiConsole.Ask<long>("[cyan]Enter the thread ID:[/]");
    }

    private static bool ThreadAlreadyExists(List<WatchedThread> watchedThreads, string board, long threadId)
    {
        return watchedThreads.Any(t => 
            t.Board.Equals(board, StringComparison.OrdinalIgnoreCase) && 
            t.ThreadId == threadId);
    }

    private async Task FetchAndAddThreadAsync(List<WatchedThread> watchedThreads, string board, long threadId)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[blue]Fetching thread {threadId} from /{board}/...[/]");

        var newThread = new WatchedThread
        {
            Board = board,
            ThreadId = threadId,
            LastChecked = DateTime.MinValue
        };

        var fetchResult = await AnsiConsole.Status()
            .StartAsync("Fetching thread data...", async ctx =>
            {
                return await _threadFetchService.FetchThreadAsync(newThread);
            });

        if (fetchResult.IsSuccess && fetchResult.ThreadData != null)
        {
            var subject = fetchResult.ThreadData.Posts.FirstOrDefault()?.Subject ?? string.Empty;
            newThread.Subject = subject;

            var mediaCount = fetchResult.ThreadData.Posts.Count(p => p.HasMedia);

            watchedThreads.Add(newThread);
            _watchedThreadService.SaveWatchedThreads(watchedThreads);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]? Thread added successfully![/]");
            AnsiConsole.MarkupLine($"[dim]Board:[/] [cyan]{FileNameSanitizer.EscapeMarkup(board)}[/]");
            AnsiConsole.MarkupLine($"[dim]Thread ID:[/] [cyan]{threadId}[/]");
            AnsiConsole.MarkupLine($"[dim]Subject:[/] [cyan]{FileNameSanitizer.EscapeMarkup(subject.Length > 0 ? subject : "No Subject")}[/]");
            AnsiConsole.MarkupLine($"[dim]Media Files:[/] [cyan]{mediaCount}[/]");
        }
        else if (fetchResult.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]? Thread {threadId} not found on /{board}/.[/]");
            AnsiConsole.MarkupLine("[yellow]Please check the board code and thread ID.[/]");
        }
        else
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]? Failed to fetch thread. Status: {fetchResult.StatusCode}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to return...[/]");
        Console.ReadKey(true);
    }

    public void ListAndDeleteThreads()
    {
        var watchedThreads = _watchedThreadService.ReadWatchedThreads();

        if (!HasWatchedThreads(watchedThreads))
        {
            DisplayNoThreadsMessage();
            return;
        }

        while (true)
        {
            AnsiConsole.Clear();
            CliPrint.PrintTitle();
            AnsiConsole.MarkupLine("[bold cyan]Manage Watched Threads[/]\n");

            DisplayThreadsTable(watchedThreads!);

            var action = PromptForAction(watchedThreads!);

            if (action == "back")
                break;

            if (action == "delete")
            {
                var threadsToDelete = PromptForThreadsToDelete(watchedThreads!);

                if (threadsToDelete.Count > 0)
                {
                    DeleteThreads(watchedThreads!, threadsToDelete);
                    _watchedThreadService.SaveWatchedThreads(watchedThreads!);

                    if (watchedThreads!.Count == 0)
                    {
                        DisplayAllThreadsDeletedMessage();
                        break;
                    }
                }
            }
        }
    }

    private static bool HasWatchedThreads(List<WatchedThread>? watchedThreads)
    {
        return watchedThreads != null && watchedThreads.Count > 0;
    }

    private static void DisplayNoThreadsMessage()
    {
        AnsiConsole.MarkupLine("[yellow]No threads found in watched-threads.json[/]");
        AnsiConsole.MarkupLine("[dim]Press any key to return...[/]");
        Console.ReadKey(true);
    }

    private static void DisplayThreadsTable(List<WatchedThread> watchedThreads)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("[bold]#[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Board[/]"));
        table.AddColumn(new TableColumn("[bold]Thread ID[/]"));
        table.AddColumn(new TableColumn("[bold]Subject[/]"));
        table.AddColumn(new TableColumn("[bold]Downloaded Files[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Errors[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Last Checked[/]"));

        for (int i = 0; i < watchedThreads.Count; i++)
        {
            var thread = watchedThreads[i];
            var index = (i + 1).ToString();
            var board = FileNameSanitizer.EscapeMarkup(thread.Board);
            var threadId = thread.ThreadId.ToString();
            var subject = string.IsNullOrWhiteSpace(thread.Subject) 
                ? "[dim]No Subject[/]" 
                : FileNameSanitizer.EscapeMarkup(thread.Subject);
            var files = thread.TotalDownloadedFiles.ToString();
            var errors = thread.ErrorCount > 0 
                ? $"[red]{thread.ErrorCount}[/]" 
                : "[green]0[/]";
            var lastChecked = thread.LastChecked == DateTime.MinValue 
                ? "[dim]Never[/]" 
                : FormatLastChecked(thread.LastChecked);

            table.AddRow(index, board, threadId, subject, files, errors, lastChecked);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static string FormatLastChecked(DateTime lastChecked)
    {
        var timeSpan = DateTime.UtcNow - lastChecked;

        if (timeSpan.TotalMinutes < 1)
            return "[green]Just now[/]";
        
        if (timeSpan.TotalMinutes < 60)
            return $"[green]{(int)timeSpan.TotalMinutes}m ago[/]";
        
        if (timeSpan.TotalHours < 24)
            return $"[yellow]{(int)timeSpan.TotalHours}h ago[/]";
        
        if (timeSpan.TotalDays < 7)
            return $"[yellow]{(int)timeSpan.TotalDays}d ago[/]";

        return $"[dim]{lastChecked:yyyy-MM-dd}[/]";
    }

    private static string PromptForAction(List<WatchedThread> watchedThreads)
    {
        var choices = new List<string> { "Delete Threads", "Back to Menu" };

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]What would you like to do?[/]")
                .AddChoices(choices));

        return selection switch
        {
            "Delete Threads" => "delete",
            "Back to Menu" => "back",
            _ => "back"
        };
    }

    private static List<WatchedThread> PromptForThreadsToDelete(List<WatchedThread> watchedThreads)
    {
        var threadChoices = watchedThreads.Select((t, i) => 
        {
            var subject = string.IsNullOrWhiteSpace(t.Subject) ? "No Subject" : t.Subject;
            return $"{i + 1}. /{t.Board}/ - {t.ThreadId} - {subject}";
        }).ToList();

        threadChoices.Add("[red]Cancel[/]");

        var selections = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[red]Select threads to delete:[/]")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more threads)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle a thread, [green]<enter>[/] to confirm)[/]")
                .AddChoices(threadChoices));

        if (selections.Contains("[red]Cancel[/]") || selections.Count == 0)
        {
            return new List<WatchedThread>();
        }

        var threadsToDelete = new List<WatchedThread>();
        foreach (var selection in selections)
        {
            var indexStr = selection.Split('.')[0];
            if (int.TryParse(indexStr, out var index) && index > 0 && index <= watchedThreads.Count)
            {
                threadsToDelete.Add(watchedThreads[index - 1]);
            }
        }

        if (threadsToDelete.Count > 0)
        {
            var confirm = AnsiConsole.Confirm(
                $"[red]Are you sure you want to delete {threadsToDelete.Count} thread(s)?[/]");

            if (!confirm)
            {
                return new List<WatchedThread>();
            }
        }

        return threadsToDelete;
    }

    private static void DeleteThreads(List<WatchedThread> watchedThreads, List<WatchedThread> threadsToDelete)
    {
        foreach (var thread in threadsToDelete)
        {
            watchedThreads.Remove(thread);
            var subject = string.IsNullOrWhiteSpace(thread.Subject) ? "No Subject" : thread.Subject;
            AnsiConsole.MarkupLine($"[red]Deleted:[/] /{FileNameSanitizer.EscapeMarkup(thread.Board)}/ - {thread.ThreadId} - {FileNameSanitizer.EscapeMarkup(subject)}");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Successfully deleted {threadsToDelete.Count} thread(s)![/]");
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private static void DisplayAllThreadsDeletedMessage()
    {
        AnsiConsole.MarkupLine("[yellow]All threads have been deleted.[/]");
        AnsiConsole.MarkupLine("[dim]Press any key to return to menu...[/]");
        Console.ReadKey(true);
    }
}
