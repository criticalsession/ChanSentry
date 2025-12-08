using ChanSentry.Common.Models;

namespace ChanSentry.Tests.Helpers
{
    /// <summary>
    /// Helper class to provide sample JSON data for unit tests
    /// </summary>
    public static class TestDataHelper
    {
        #region Board JSON Samples

        /// <summary>
        /// Returns a single board JSON object
        /// </summary>
        public static string GetSingleBoardJson(
            string board = "g",
            string title = "Technology",
            int wsBoard = 1,
            string metaDescription = "Discussion of technology and related topics.")
        {
            return $$"""
            {
                "board": "{{board}}",
                "title": "{{title}}",
                "ws_board": {{wsBoard}},
                "meta_description": "{{metaDescription}}"
            }
            """;
        }

        /// <summary>
        /// Returns a boards JSON object with multiple boards
        /// </summary>
        public static string GetMultipleBoardsJson()
        {
            return """
            {
                "boards": [
                    {
                        "board": "g",
                        "title": "Technology",
                        "ws_board": 1,
                        "meta_description": "Discussion of technology."
                    },
                    {
                        "board": "pol",
                        "title": "Politically Incorrect",
                        "ws_board": 0,
                        "meta_description": "Politics discussion."
                    }
                ]
            }
            """;
        }

        /// <summary>
        /// Returns a partial board JSON object (only some fields)
        /// </summary>
        public static string GetPartialBoardJson()
        {
            return """
            {
                "board": "g",
                "title": "Technology"
            }
            """;
        }

        /// <summary>
        /// Returns a board JSON object with extra unknown fields
        /// </summary>
        public static string GetBoardJsonWithExtraFields()
        {
            return """
            {
                "board": "g",
                "title": "Technology",
                "ws_board": 1,
                "meta_description": "Discussion of technology.",
                "extra_field": "should be ignored",
                "another_unknown_field": 999
            }
            """;
        }

        #endregion

        #region Catalog JSON Samples

        /// <summary>
        /// Returns a single catalog thread JSON object
        /// </summary>
        public static string GetSingleCatalogThreadJson(
            long no = 98765432,
            string sub = "Test Thread",
            string com = "This is a test comment",
            int replies = 42,
            int images = 10)
        {
            return $$"""
            {
                "no": {{no}},
                "sub": "{{sub}}",
                "com": "{{com}}",
                "replies": {{replies}},
                "images": {{images}}
            }
            """;
        }

        /// <summary>
        /// Returns a catalog threads JSON object with multiple threads
        /// </summary>
        public static string GetMultipleCatalogThreadsJson()
        {
            return """
            {
                "page": 0,
                "threads": [
                    {
                        "no": 98765432,
                        "sub": "Thread 1",
                        "com": "Comment 1",
                        "replies": 10,
                        "images": 5
                    },
                    {
                        "no": 98765433,
                        "sub": "Thread 2",
                        "com": "Comment 2",
                        "replies": 20,
                        "images": 8
                    }
                ]
            }
            """;
        }

        #endregion

        #region Thread and Post JSON Samples

        /// <summary>
        /// Returns a single post JSON object with all fields populated
        /// </summary>
        public static string GetPostJsonWithAllFields(
            string filename = "test_image",
            long tim = 1745612650141704,
            string ext = ".png",
            long time = 1745612650)
        {
            return $$"""
            {
                "filename": "{{filename}}",
                "tim": {{tim}},
                "ext": "{{ext}}",
                "time": {{time}}
            }
            """;
        }

        /// <summary>
        /// Returns a single post JSON object with nullable fields set to null
        /// </summary>
        public static string GetPostJsonWithNullableFields()
        {
            return """
            {
                "filename": null,
                "tim": null,
                "ext": null,
                "time": 1745612650
            }
            """;
        }

        /// <summary>
        /// Returns a thread JSON object with multiple posts
        /// </summary>
        public static string GetThreadJsonWithMultiplePosts()
        {
            return """
            {
                "posts": [
                    {
                        "filename": "image1",
                        "tim": 1745612650141704,
                        "ext": ".png",
                        "time": 1745612650
                    },
                    {
                        "filename": null,
                        "tim": null,
                        "ext": null,
                        "time": 1745612660
                    }
                ]
            }
            """;
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Returns an empty JSON object
        /// </summary>
        public static string GetEmptyJson() => "{}";

        /// <summary>
        /// Returns an empty string
        /// </summary>
        public static string GetEmptyString() => "";

        /// <summary>
        /// Returns invalid JSON
        /// </summary>
        public static string GetInvalidJson() => "{ invalid json }";

        /// <summary>
        /// Returns null (for testing null handling)
        /// </summary>
        public static string? GetNullJson() => null;

        #endregion

        #region DownloadHandler Test Data

        /// <summary>
        /// Returns a thread JSON with media posts
        /// </summary>
        public static string GetThreadJsonWithMediaPosts(int mediaPostCount = 2)
        {
            var posts = new List<string>();
            for (int i = 0; i < mediaPostCount; i++)
            {
                posts.Add($$"""
                {
                    "filename": "image{{i}}",
                    "tim": {{1234567890 + i}},
                    "ext": ".jpg",
                    "time": {{1638360000 + (i * 30)}}
                }
                """);
            }
            return $$"""
            {
                "posts": [{{string.Join(",", posts)}}]
            }
            """;
        }

        /// <summary>
        /// Returns a thread JSON with mixed media and non-media posts
        /// </summary>
        public static string GetThreadJsonWithMixedPosts()
        {
            return """
            {
                "posts": [
                    {
                        "filename": "image1",
                        "tim": 1234567890,
                        "ext": ".jpg",
                        "time": 1638360000
                    },
                    {
                        "time": 1638360050
                    },
                    {
                        "filename": "image2",
                        "tim": 9876543210,
                        "ext": ".png",
                        "time": 1638360100
                    },
                    {
                        "time": 1638360150
                    }
                ]
            }
            """;
        }

        /// <summary>
        /// Returns a complex thread JSON with multiple types of posts
        /// </summary>
        public static string GetComplexThreadJson()
        {
            return """
            {
                "posts": [
                    {
                        "filename": "op_image",
                        "tim": 1000000001,
                        "ext": ".jpg",
                        "time": 1638360000
                    },
                    {
                        "time": 1638360030
                    },
                    {
                        "filename": "reply_image1",
                        "tim": 1000000002,
                        "ext": ".png",
                        "time": 1638360060
                    },
                    {
                        "time": 1638360090
                    },
                    {
                        "filename": "reply_image2",
                        "tim": 1000000003,
                        "ext": ".gif",
                        "time": 1638360120
                    },
                    {
                        "filename": "reply_image3",
                        "tim": 1000000004,
                        "ext": ".webm",
                        "time": 1638360150
                    },
                    {
                        "time": 1638360180
                    }
                ]
            }
            """;
        }

        /// <summary>
        /// Returns a watched thread JSON with specified properties
        /// </summary>
        public static string GetWatchedThreadJson(
            string board = "g",
            long threadId = 12345,
            string subject = "Test Thread",
            int errorCount = 0,
            int totalDownloadedFiles = 5)
        {
            return $$"""
            {
                "Board": "{{board}}",
                "ThreadId": {{threadId}},
                "Subject": "{{subject}}",
                "ErrorCount": {{errorCount}},
                "TotalDownloadedFiles": {{totalDownloadedFiles}},
                "LastChecked": "2024-01-01T00:00:00Z"
            }
            """;
        }

        /// <summary>
        /// Returns a watched threads JSON array
        /// </summary>
        public static string GetWatchedThreadsJson()
        {
            return """
            [
                {
                    "Board": "g",
                    "ThreadId": 12345,
                    "Subject": "Test Thread 1",
                    "ErrorCount": 0,
                    "TotalDownloadedFiles": 5,
                    "LastChecked": "2024-01-01T00:00:00Z"
                },
                {
                    "Board": "pol",
                    "ThreadId": 67890,
                    "Subject": "Test Thread 2",
                    "ErrorCount": 1,
                    "TotalDownloadedFiles": 3,
                    "LastChecked": "2024-01-01T00:00:00Z"
                }
            ]
            """;
        }

        /// <summary>
        /// Returns an empty watched threads JSON array
        /// </summary>
        public static string GetEmptyWatchedThreadsJson() => "[]";

        /// <summary>
        /// Common test board codes
        /// </summary>
        public static string[] GetTestBoardCodes() => new[] { "g", "pol", "b", "fit", "wg", "tv", "vg" };

        /// <summary>
        /// Common test file extensions
        /// </summary>
        public static string[] GetTestFileExtensions() => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webm", ".mp4", ".pdf" };

        /// <summary>
        /// Common test thread IDs
        /// </summary>
        public static string[] GetTestThreadIds() => new[] { "12345", "67890", "11111", "22222", "33333" };

        /// <summary>
        /// Creates a test Post with media
        /// </summary>
        public static Post CreateTestPostWithMedia(
            long internalFileId = 1234567890,
            string extension = ".jpg",
            string fileName = "testfile")
        {
            return new Post
            {
                InternalFileIdentifier = internalFileId,
                FileExtension = extension,
                FileName = fileName,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// Creates a test Post without media
        /// </summary>
        public static Post CreateTestPostWithoutMedia()
        {
            return new Post
            {
                InternalFileIdentifier = null,
                FileExtension = null,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// Creates a test WatchedThread
        /// </summary>
        public static WatchedThread CreateTestWatchedThread(
            string board = "g",
            long threadId = 12345,
            string subject = "Test Thread",
            int errorCount = 0,
            int totalDownloadedFiles = 0)
        {
            return new WatchedThread
            {
                Board = board,
                ThreadId = threadId,
                Subject = subject,
                ErrorCount = errorCount,
                TotalDownloadedFiles = totalDownloadedFiles,
                LastChecked = DateTime.MinValue
            };
        }

        #endregion
    }
}
