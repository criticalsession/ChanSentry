using System;
using ChanSentry.Common.Helpers;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace ChanSentry.Watcher.Services;

public class ThreadService(ThreadFetchService threadFetchService, LogHelper logHelper) : BackgroundService
{
    Dictionary<long, string> threadsToWatch = new()
    {
        { 8122678, "wg" },
        { 8116493, "wg" }
    };

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logHelper.LogInfo("Starting Watcher...");

        var runTask = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var thread in threadsToWatch)
                {
                    try
                    {
                        await threadFetchService.Get(thread.Value, thread.Key);
                    }
                    catch (Exception ex)
                    {
                        logHelper.LogError($"Error fetching thread {thread.Key}: {ex.Message}");
                        // TODO: remove thread from watch list
                    }

                    await Task.Delay(2000, cancellationToken);
                }
            }
        }, cancellationToken);

        logHelper.LogSuccess("Watcher started.");
        await runTask;
    }
}
