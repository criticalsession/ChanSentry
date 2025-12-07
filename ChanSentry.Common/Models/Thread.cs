using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace ChanSentry.Common.Models;

public class Thread : IApiModel
{
    [JsonPropertyName("posts")]
    public List<Post> Posts { get; set; } = new();
}

public class Post : IApiModel
{
    [JsonPropertyName("filename")]
    public string? FileName { get; set; }

    [JsonPropertyName("tim")]
    public long? InternalFileIdentifier { get; set; }

    [JsonPropertyName("ext")]
    public string? FileExtension { get; set; }

    [JsonPropertyName("time")]
    public long Timestamp { get; set; }

    public string? GetFileUrl(string boardCode) => InternalFileIdentifier is null || FileExtension is null 
        ? null 
        : string.Format(Constants.Urls.FileUrlTemplate, boardCode, InternalFileIdentifier, FileExtension);
}