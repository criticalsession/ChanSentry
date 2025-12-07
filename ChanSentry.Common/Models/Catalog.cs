using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ChanSentry.Common.Models;

public class CatalogThreads
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("threads")]
    public List<CatalogThread> ThreadList { get; set; } = new();
}

public class CatalogThread
{
    [JsonPropertyName("no")]
    public int ThreadId { get; set; }

    [JsonPropertyName("sub")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("com")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("replies")]
    public int ReplyCount { get; set; } = 0;

    [JsonPropertyName("images")]
    public int ImageCount { get; set; } = 0;
}