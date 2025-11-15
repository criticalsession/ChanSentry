using System;

namespace ChanSentry.Common.Models;

public class ThreadPost
{
    public int no { get; set; }
    public string filename { get; set; } = string.Empty;
    public string ext { get; set; } = string.Empty;
    public long tim { get; set; }

    public string GetFullUrl(string boardCode) => $"https://i.4cdn.org/{boardCode}/{tim}{ext}";
}
