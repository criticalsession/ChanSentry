using ChanSentry.Common.Models;

namespace ChanSentry.CLI.Services.Interfaces;

public interface IWatchedThreadService
{
    List<WatchedThread>? ReadWatchedThreads();
    void SaveWatchedThreads(List<WatchedThread> watchedThreads);
    List<WatchedThread> RemoveFailedThreads(List<WatchedThread> watchedThreads);
}
