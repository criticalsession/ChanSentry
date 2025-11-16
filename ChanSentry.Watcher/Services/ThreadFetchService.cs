using System.Text.Json;
using ChanSentry.Common.Helpers;
using Spectre.Console;

namespace ChanSentry.Watcher.Services;

public class ThreadFetchService(IHttpClientFactory httpClientFactory, DataHelper dataHelper, FileHelper fileHelper, LogHelper logHelper)
{
    public async Task Get(string boardCode, long threadId)
    {
        var shortName = $"{boardCode}/{threadId}";

        logHelper.LogInfo($"Checking thread '{shortName}'");

        var thread = await GetThreadAsync(boardCode, threadId);
        if (thread == null || thread.posts.Count == 0)
        {
            logHelper.LogInfo("No posts found in thread.");
            return;
        }

        thread.id = threadId;
        thread.boardCode = boardCode;
        thread.lastFetched = DateTime.UtcNow;

        fileHelper.CheckAndBuildDirectories(boardCode, threadId);

        var urls = dataHelper.GetFiles(thread).ToList();
        if (!urls.Any())
        {
            logHelper.LogInfo("No new files to download.");
            return;
        }

        logHelper.LogInfo($"Downloading {urls.Count} new files from {shortName}");

        var cdnHttpClient = httpClientFactory.CreateClient("4chancdn");

        // TODO: move to settings
        const int maxConcurrency = 4;
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var downloadTasks = new List<Task>();

        foreach (var url in urls)
        {
            var u = url;
            await semaphore.WaitAsync();

            downloadTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var fileResponse = await cdnHttpClient.GetAsync(u);
                    await fileHelper.DownloadAsync(fileResponse, boardCode, threadId, u);
                }
                catch (Exception ex)
                {
                    logHelper.LogError($"Failed to download '{u}': {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        try
        {
            await Task.WhenAll(downloadTasks);
        }
        finally
        {
            logHelper.LogSuccess($"All new files downloaded from '{shortName}'");
        }
    }

    private async Task<Common.Models.Thread?> GetThreadAsync(string boardCode, long threadId)
    {
        var httpClient = httpClientFactory.CreateClient("4chan");
        var response = await httpClient.GetAsync($"{boardCode}/thread/{threadId}.json");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var thread = JsonSerializer.Deserialize<Common.Models.Thread>(content);

        return thread;
    }
}
