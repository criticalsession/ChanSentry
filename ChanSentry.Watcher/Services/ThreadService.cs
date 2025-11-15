using System;
using Microsoft.Extensions.Hosting;

namespace ChanSentry.Watcher.Services;

public class ThreadService(ThreadFetchService threadFetchService) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting ThreadService...");
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await threadFetchService.Get();
                await Task.Delay(5000, cancellationToken);
            }
        }, cancellationToken);
        Console.WriteLine("ThreadService is running.");
        return Task.CompletedTask;
    }
}
