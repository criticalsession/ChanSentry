namespace ChanSentry.Common.Models;

public class WatchedThread
{
    public string Board { get; set; } = string.Empty;
    public long ThreadId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int ErrorCount { get; set; } = 0;
    public int TotalDownloadedFiles { get; set; } = 0;
    public DateTime LastChecked { get; set; } = DateTime.MinValue;
}
