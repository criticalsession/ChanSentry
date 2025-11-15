using System.Text.Json;
using ChanSentry.Common.Helpers;

namespace ChanSentry.Watcher.Services;

public class ThreadFetchService(IHttpClientFactory httpClientFactory, DataHelper dataHelper, FileHelper fileHelper)
{
    public async Task Get(string boardCode, long threadId)
    {
        var thread = await GetThreadAsync(boardCode, threadId);
        if (thread == null || thread.posts.Count == 0)
        {
            Console.WriteLine("No posts found in thread.");
            return;
        }

        thread.id = threadId;
        thread.boardCode = boardCode;
        thread.lastFetched = DateTime.UtcNow;

        fileHelper.CheckAndBuildDirectories(boardCode, threadId);

        var cdnHttpClient = httpClientFactory.CreateClient("4chancdn");
        foreach (var url in dataHelper.GetFiles(thread))
        {
            Console.WriteLine($"Downloading file: {url} [{boardCode}/{threadId}]");
            var fileResponse = await cdnHttpClient.GetAsync(url);
            await fileHelper.DownloadAsync(fileResponse, boardCode, threadId, url);
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
