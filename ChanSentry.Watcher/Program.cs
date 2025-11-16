using ChanSentry.Common.Helpers;
using ChanSentry.Watcher.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient("4chan", httpClient =>
        {
            httpClient.BaseAddress = new Uri("https://a.4cdn.org/");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        });

        services.AddHttpClient("4chancdn", httpClient =>
        {
            httpClient.BaseAddress = new Uri("https://i.4cdn.org/");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        });

        services.AddSingleton<DataHelper>();
        services.AddSingleton<FileHelper>();
        services.AddSingleton<LogHelper>();

        services.AddScoped<ThreadFetchService>();
        services.AddHostedService<ThreadService>();
    })
    .Build();

await host.RunAsync();
