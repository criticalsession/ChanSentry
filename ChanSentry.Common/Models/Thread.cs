using System;

namespace ChanSentry.Common.Models;

public class Thread
{
    public List<ThreadPost> posts { get; set; } = [];
    public long id { get; set; }
    public DateTime? lastFetched { get; set; }
    public string boardCode { get; set; } = string.Empty;
}
