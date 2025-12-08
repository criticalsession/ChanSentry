using System.Text.Json.Serialization;

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

    public bool HasMedia => InternalFileIdentifier.HasValue && FileExtension is not null;

    public string? GetFileUrl(string boardCode) => HasMedia
        ? string.Format(Constants.Urls.FileUrlTemplate, boardCode, InternalFileIdentifier, FileExtension)
        : null;
}