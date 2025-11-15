using System.Text.Json;
using ChanSentry.Common;

namespace ChanSentry.Watcher.Services;

public class ThreadFetchService(IHttpClientFactory httpClientFactory, DataHelper dataHelper)
{
    public async Task Get(string boardCode, long threadId)
    {
        var httpClient = httpClientFactory.CreateClient("4chan");
        var response = await httpClient.GetAsync($"{boardCode}/thread/{threadId}.json");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var thread = JsonSerializer.Deserialize<Common.Models.Thread>(content);

        if (thread == null || thread.posts.Count == 0)
        {
            Console.WriteLine("No posts found in thread.");
            return;
        }

        thread.id = threadId;
        thread.boardCode = boardCode;
        thread.lastFetched = DateTime.UtcNow;

        if (!Directory.Exists("Downloads"))
        {
            Console.WriteLine("Creating Downloads directory...");
            Directory.CreateDirectory("Downloads");
        }

        if (!Directory.Exists("Downloads/" + boardCode))
        {
            Console.WriteLine($"Creating Downloads/{boardCode} directory...");
            Directory.CreateDirectory("Downloads/" + boardCode);
        }

        if (!Directory.Exists($"Downloads/{boardCode}/{threadId}"))
        {
            Console.WriteLine($"Creating Downloads/{boardCode}/{threadId} directory...");
            Directory.CreateDirectory($"Downloads/{boardCode}/{threadId}");
        }

        var cdnHttpClient = httpClientFactory.CreateClient("4chancdn");
        foreach (var url in dataHelper.GetFiles(thread))
        {
            Console.WriteLine($"Downloading file: {url} [{boardCode}/{threadId}]");
            var fileResponse = await cdnHttpClient.GetAsync(url);
            if (fileResponse.IsSuccessStatusCode)
            {
                using (FileStream fs = new FileStream($"Downloads/{boardCode}/{threadId}/{Path.GetFileName(url)}", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await fileResponse.Content.CopyToAsync(fs);
                }
            }
        }
    }
}
