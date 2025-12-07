using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ChanSentry.Common.Models;

public class Boards : IApiModel
{
    [JsonPropertyName("boards")]
    public List<Board> BoardsList { get; set; } = new();
}

public class Board : IApiModel
{
    [JsonPropertyName("board")]
    public string BoardCode { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("ws_board")]
    public int IsWorkSafe { get; set; }

    [JsonPropertyName("meta_description")]
    public string Description { get; set; } = string.Empty;
}