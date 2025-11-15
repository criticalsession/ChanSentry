using System;
using System.Text.Json;

namespace ChanSentry.Watcher.Services;

public class ThreadFetchService(IHttpClientFactory httpClientFactory)
{
    public async Task Get()
    {
        Console.WriteLine("Fetching thread data...");
        var httpClient = httpClientFactory.CreateClient("4chan");
        var response = await httpClient.GetAsync("wg/thread/8117724.json");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var thread = JsonSerializer.Deserialize<Common.Models.Thread>(content);

        if (thread == null || thread.posts.Count == 0)
        {
            Console.WriteLine("No posts found in the thread.");
            return;
        }

        thread.boardCode = "wg";
        thread.lastFetched = DateTime.UtcNow;
    }
}
