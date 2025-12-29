using ChanSentry.Cli.Utils;
using ChanSentry.Common.Helpers;
using ChanSentry.Common.Models;
using Spectre.Console;
using System.Text.Json;

namespace ChanSentry.CLI.Handlers;

public class DownloadHandler
{
    private const string WatchedThreadsFile = "watched-threads.json";

    public async Task StartAsync()
    {
        var running = true;
        List<WatchedThread>? watchedThreads = ReadWatchedThreadsFromFile();

        while (running)
        {
            AnsiConsole.Clear();
            CliPrint.PrintTitle();

            AnsiConsole.MarkupLine("[bold cyan]Downloader Running...[/]");
            AnsiConsole.MarkupLine("[dim]Press ESC or Q to return to main menu[/]\n");

            // Read and deserialize the watched threads
            try
            {
                if (watchedThreads == null || watchedThreads.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No threads found in watched-threads.json[/]");
                    AnsiConsole.MarkupLine("[dim]Press any key to return to main menu...[/]");
                    Console.ReadKey(true);
                    return;
                }
                else
                {
                    // Process each thread
                    int threadIdx = 0;
                    foreach (var thread in watchedThreads)
                    {
                        if (thread.ErrorCount >= 3) continue;

                        AnsiConsole.MarkupLine($"[grey]Checking: [/]{thread.Board}/{thread.ThreadId} - [green]{thread.Subject}[/]");

                        using var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "ChanSentry/1.0");
                        httpClient.DefaultRequestHeaders.Add("If-Modified-Since", thread.LastChecked.ToString("R"));

                        thread.LastChecked = DateTime.UtcNow;

                        var response = await httpClient.GetAsync(string.Format(Common.Constants.Urls.ThreadUrlTemplate, thread.Board, thread.ThreadId));
                        if (response.IsSuccessStatusCode)
                        {
                            (List<Post> mediaPosts, List<Post> newMedia) = await GetNewMediaListAsync(thread, response);

                            await DownloadMediaFilesAsync(newMedia, thread.Board, thread.ThreadId.ToString());

                            thread.TotalDownloadedFiles = mediaPosts.Count;
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                        {
                            AnsiConsole.MarkupLine($"[blue]Thread {thread.ThreadId} on /{thread.Board}/ has not been modified since last check.[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]Failed to fetch thread {thread.ThreadId} on /{thread.Board}/ - Status Code: {response.StatusCode} (Total Errors: {thread.ErrorCount + 1}/3)[/]");
                            thread.ErrorCount++;

                            if (thread.ErrorCount >= 3)
                            {
                                AnsiConsole.MarkupLine($"[red]Thread has failed {thread.ErrorCount} times and will be deleted from Watched Threads list.[/]");
                            }
                        }

                        UpdateWatchedThreadsFile(watchedThreads);

                        if (threadIdx < watchedThreads.Count - 1)
                        {
                            if (!await CountdownWithExitCheckAsync(2))
                            {
                                running = false;
                                break;
                            }
                        }

                        threadIdx++;
                    }

                    watchedThreads = watchedThreads.Where(t => t.ErrorCount < 3).ToList();

                    if (running && !await CountdownWithExitCheckAsync(10))
                    {
                        running = false;
                        break;
                    }
                }
            } 
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error reading watched threads: {ex.Message}[/]");
                AnsiConsole.MarkupLine("[dim]Press any key to return to main menu...[/]");
                Console.ReadKey(true);
                return;
            }

            if (CheckExitKeys())
            {
                running = false;
                break;
            }
        }

        if (watchedThreads is not null)
        {
            AnsiConsole.MarkupLine("\n[dim]Updating Watched Threads list...[/]");
            UpdateWatchedThreadsFile(watchedThreads);
        }

        AnsiConsole.MarkupLine("[dim]Returning to main menu...[/]");
        System.Threading.Thread.Sleep(500);
    }

    private static async Task<(List<Post> mediaPosts, List<Post> newMedia)> GetNewMediaListAsync(WatchedThread thread, HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        AnsiConsole.MarkupLine($"[blue]Successfully fetched thread {thread.ThreadId} on /{thread.Board}/[/]");

        var threadData = JsonHelper.Deserialize<Common.Models.Thread>(content);
        var mediaPosts = threadData.Posts.Where(post => post.HasMedia).ToList();

        var newMedia = mediaPosts.Skip(thread.TotalDownloadedFiles).ToList();
        AnsiConsole.MarkupLine($"[green]Found {newMedia.Count} new media files in thread {thread.ThreadId}[/]");
        return (mediaPosts, newMedia);
    }

    private static List<WatchedThread>? ReadWatchedThreadsFromFile()
    {
        List<WatchedThread>? watchedThreads = null;

        if (!File.Exists(WatchedThreadsFile))
        {
            File.Create(WatchedThreadsFile).Dispose();
            File.WriteAllLines(WatchedThreadsFile, ["[]"]);
        }

        var json = File.ReadAllText(WatchedThreadsFile);
        watchedThreads = JsonSerializer.Deserialize<List<WatchedThread>>(json);
        return watchedThreads;
    }

    private void UpdateWatchedThreadsFile(List<WatchedThread>? watchedThreads)
    {
        if (watchedThreads is null) return;

        File.WriteAllText(WatchedThreadsFile, JsonSerializer.Serialize(watchedThreads));
    }

    private async Task<bool> CountdownWithExitCheckAsync(int seconds)
    {
        var status = AnsiConsole.Status();
        for (int i = seconds; i > 0; i--)
        {
            AnsiConsole.Markup($"\r[dim]Next check in {(i >= 10 ? i : (" " + i))} second{(i != 1 ? "s" : " ")}... (Press ESC or Q to exit)[/]");
            await Task.Delay(1000);

            if (CheckExitKeys())
            {
                AnsiConsole.WriteLine();
                return false;
            }
        }
        AnsiConsole.WriteLine();
        return true;
    }

    private bool CheckExitKeys()
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
            {
                return true;
            }
        }

        return false;
    }

    private async Task DownloadMediaFilesAsync(List<Post> posts, string boardCode, string threadId)
    {
        var downloadPath = Path.Combine("downloads", boardCode, threadId);
        Directory.CreateDirectory(downloadPath);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "ChanSentry/1.0");

        foreach (var post in posts)
        {
            try
            {
                var fileUrl = post.GetFileUrl(boardCode);
                if (string.IsNullOrEmpty(fileUrl))
                    continue;

                var sanitizedFileName = FileNameSanitizer.Sanitize(post.FileName);
                var fileName = $"{(!string.IsNullOrEmpty(sanitizedFileName) ? sanitizedFileName + " - " : "")}{post.InternalFileIdentifier}{post.FileExtension}";
                var filePath = Path.Combine(downloadPath, fileName);

                // Skip if file already exists
                if (File.Exists(filePath))
                {
                    var displayFileName = FileNameSanitizer.EscapeMarkup(fileName);
                    AnsiConsole.MarkupLine($"[grey]> File {displayFileName} already exists[/]");
                    continue;
                }

                var response = await httpClient.GetAsync(fileUrl);

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(filePath, fileBytes);
                    var displayFileName = FileNameSanitizer.EscapeMarkup(fileName);
                    AnsiConsole.MarkupLine($"[green]> Downloaded {displayFileName}[/]");
                }
                else
                {
                    var displayFileName = FileNameSanitizer.EscapeMarkup(fileName);
                    AnsiConsole.MarkupLine($"[red]> Failed to download {displayFileName} - Status: {response.StatusCode}[/]");
                }

                // Be respectful to the server
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]> Error downloading file: {ex.Message}[/]");
            }
        }
    }
}