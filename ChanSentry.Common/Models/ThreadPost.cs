using System;

namespace ChanSentry.Common.Models;

public class ThreadPost
{
    public int no { get; set; }
    public string filename { get; set; }
    public string ext { get; set; }
    public string tim { get; set; }

    public string GetFullUrl(string boardCode) => $"https://i.4cdn.org/{boardCode}/{tim}{ext}";
}
