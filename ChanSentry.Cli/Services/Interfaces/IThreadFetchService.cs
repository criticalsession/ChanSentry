using ChanSentry.Common.Models;

namespace ChanSentry.CLI.Services.Interfaces;

public interface IThreadFetchService
{
    Task<ThreadFetchResult> FetchThreadAsync(WatchedThread thread);
}
