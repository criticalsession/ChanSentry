using System;
using System.Text.Json;
using ChanSentry.Common;
using Microsoft.Extensions.Hosting.Internal;

namespace ChanSentry.Watcher.Services;

public class ThreadFetchService(IHttpClientFactory httpClientFactory, DataHelper dataHelper)
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

        var cdnHttpClient = httpClientFactory.CreateClient("4chancdn");
        foreach (var url in dataHelper.GetFiles(thread))
        {
            var fileResponse = await cdnHttpClient.GetAsync(url);
            if (fileResponse.IsSuccessStatusCode)
            {
                using (FileStream fs = new FileStream($"Downloads/{Path.GetFileName(url)}", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await fileResponse.Content.CopyToAsync(fs);
                }
            }
        }
    }
}
