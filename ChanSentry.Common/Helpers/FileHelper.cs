namespace ChanSentry.Common.Helpers;

public class FileHelper
{
    public void CheckAndBuildDirectories(string boardCode, long threadId)
    {
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
    }

    public async Task DownloadAsync(HttpResponseMessage? fileResponse, string boardCode, long threadId, string url)
    {
        if (fileResponse != null && fileResponse.IsSuccessStatusCode)
        {
            using FileStream fs = new($"Downloads/{boardCode}/{threadId}/{Path.GetFileName(url)}",
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            await fileResponse.Content.CopyToAsync(fs);
        }
    }
}
