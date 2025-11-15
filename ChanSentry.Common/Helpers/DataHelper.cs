namespace ChanSentry.Common.Helpers;

public class DataHelper
{
    public string[] GetFiles(Models.Thread thread)
    {
        ArgumentNullException.ThrowIfNull(thread);

        return thread.posts.Where(post => post.tim > 0 && post.ext != null)
            .Select(post => post.GetFullUrl(thread.boardCode))
            .ToArray();
    }
}
