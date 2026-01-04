using ChanSentry.CLI.Services.Interfaces;
using ChanSentry.Common.Models;
using System.Text.Json;

namespace ChanSentry.CLI.Services;

public class WatchedThreadService : IWatchedThreadService
{
    private const string WatchedThreadsFile = "watched-threads.json";

    public List<WatchedThread>? ReadWatchedThreads()
    {
        if (!File.Exists(WatchedThreadsFile))
        {
            CreateEmptyWatchedThreadsFile();
        }

        var json = File.ReadAllText(WatchedThreadsFile);
        return JsonSerializer.Deserialize<List<WatchedThread>>(json);
    }

    public void SaveWatchedThreads(List<WatchedThread> watchedThreads)
    {
        var json = JsonSerializer.Serialize(watchedThreads);
        File.WriteAllText(WatchedThreadsFile, json);
    }

    public List<WatchedThread> RemoveFailedThreads(List<WatchedThread> watchedThreads)
    {
        return watchedThreads.Where(t => t.ErrorCount < 3).ToList();
    }

    private static void CreateEmptyWatchedThreadsFile()
    {
        File.Create(WatchedThreadsFile).Dispose();
        File.WriteAllLines(WatchedThreadsFile, ["[]"]);
    }
}
