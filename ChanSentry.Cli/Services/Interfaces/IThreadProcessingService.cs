using ChanSentry.Common.Models;

namespace ChanSentry.CLI.Services.Interfaces;

public interface IThreadProcessingService
{
    Task ProcessThreadAsync(WatchedThread thread, List<WatchedThread> allWatchedThreads);
}
