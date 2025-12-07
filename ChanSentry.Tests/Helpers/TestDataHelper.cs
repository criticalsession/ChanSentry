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
    }
}
