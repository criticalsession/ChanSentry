using System;
using System.Collections.Generic;
using System.Text;

namespace ChanSentry.Common;

/// <summary>
/// Provides application-wide constants for the ChanSentry application.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Contains URL constants for accessing 4chan's public API endpoints and resources.
    /// </summary>
    public static class Urls
    {
        /// <summary>
        /// The base URL for 4chan's JSON API (https://a.4cdn.org).
        /// </summary>
        public const string BaseUrl = "https://a.4cdn.org";

        /// <summary>
        /// The base URL for 4chan's media/file hosting (https://i.4cdn.org).
        /// </summary>
        public const string BaseFileUrl = "https://i.4cdn.org";

        /// <summary>
        /// The URL for retrieving the list of all available boards.
        /// </summary>
        public const string BoardsListUrl = $"{BaseUrl}/boards.json";

        /// <summary>
        /// URL template for accessing a board's catalog. 
        /// Format: {0} = board name (e.g., "g", "pol").
        /// </summary>
        public const string CatalogUrlTemplate = $"{BaseUrl}/{{0}}/catalog.json";

        /// <summary>
        /// URL template for accessing a specific thread.
        /// Format: {0} = board name, {1} = thread ID.
        /// </summary>
        public const string ThreadUrlTemplate = $"{BaseUrl}/{{0}}/thread/{{1}}.json";

        /// <summary>
        /// URL template for accessing a file/media resource.
        /// Format: {0} = board name, {1} = filename, {2} = file extension.
        /// </summary>
        public const string FileUrlTemplate = $"{BaseFileUrl}/{{0}}/{{1}}{{2}}";
    }
}
