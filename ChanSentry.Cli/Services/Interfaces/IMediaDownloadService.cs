using ChanSentry.Common.Models;

namespace ChanSentry.CLI.Services.Interfaces;

public interface IMediaDownloadService
{
    Task DownloadMediaFilesAsync(List<Post> posts, WatchedThread thread);
}
