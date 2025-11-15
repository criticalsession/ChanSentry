using System;
using Microsoft.Extensions.Hosting;

namespace ChanSentry.Watcher.Services;

public class ThreadService(ThreadFetchService threadFetchService) : BackgroundService
{
    Dictionary<long, string> threadsToWatch = new()
    {
        { 8122678, "wg" },
        { 8116493, "wg" }
    };

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting ThreadService...");
        foreach (var thread in threadsToWatch)
        {
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    await threadFetchService.Get(thread.Value, thread.Key);
                    await Task.Delay(5000, cancellationToken);
                }
            }, cancellationToken);

            Task.Delay(2000, cancellationToken);
        }

        Console.WriteLine("ThreadService is running.");
        return Task.CompletedTask;
    }
}
