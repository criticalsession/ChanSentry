using ChanSentry.CLI.Services.Interfaces;
using ChanSentry.Common.Helpers;
using ChanSentry.Common.Models;
using Spectre.Console;

namespace ChanSentry.CLI.Services;

public class ThreadFetchService : IThreadFetchService
{
    private const string UserAgent = "ChanSentry/1.0";

    public async Task<ThreadFetchResult> FetchThreadAsync(WatchedThread thread)
    {
        using var httpClient = CreateHttpClient(thread.LastChecked);
        
        var url = string.Format(Common.Constants.Urls.ThreadUrlTemplate, thread.Board, thread.ThreadId);
        var response = await httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var threadData = JsonHelper.Deserialize<Common.Models.Thread>(content);
            return ThreadFetchResult.Success(threadData);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            return ThreadFetchResult.NotModified();
        }

        return ThreadFetchResult.Failed(response.StatusCode);
    }

    private static HttpClient CreateHttpClient(DateTime lastChecked)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        httpClient.DefaultRequestHeaders.Add("If-Modified-Since", lastChecked.ToString("R"));
        return httpClient;
    }
}

public class ThreadFetchResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotModified { get; init; }
    public Common.Models.Thread? ThreadData { get; init; }
    public System.Net.HttpStatusCode? StatusCode { get; init; }

    public static ThreadFetchResult Success(Common.Models.Thread threadData)
        => new() { IsSuccess = true, ThreadData = threadData };

    public static ThreadFetchResult NotModified()
        => new() { IsNotModified = true };

    public static ThreadFetchResult Failed(System.Net.HttpStatusCode statusCode)
        => new() { StatusCode = statusCode };
}
