using System;

namespace ChanSentry.Common.Models;

public class Thread
{
    public ThreadPost[] posts { get; set; }
    public DateTime? lastFetched { get; set; }
    public string boardCode { get; set; }
}
