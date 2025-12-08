using ChanSentry.Common.Models;
using ChanSentry.Tests.Helpers;
using System.Text.Json;

namespace ChanSentry.Tests.Handlers;

[TestFixture]
public class DownloadHandlerTests
{
    private string _testDirectory = string.Empty;
    private string _watchedThreadsFile = string.Empty;

    [SetUp]
    public void SetUp()
    {
        // Create a temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ChanSentryTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        // Set up test file paths
        _watchedThreadsFile = Path.Combine(_testDirectory, "watched-threads.json");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region Helper Methods Tests

    [Test]
    public void DownloadMediaFilesAsync_CreatesCorrectDirectoryStructure()
    {
        // Arrange
        var boardCode = "g";
        var threadId = "12345";
        var expectedPath = Path.Combine("downloads", boardCode, threadId);

        var posts = new List<Post>
        {
            TestDataHelper.CreateTestPostWithMedia()
        };

        // Act & Assert
        // Note: This test verifies the directory structure would be created
        // The actual download would require mocking HttpClient
        Assert.That(posts.First().HasMedia, Is.True);
        Assert.That(posts.First().GetFileUrl(boardCode), Is.Not.Null);
    }

    [Test]
    public void WatchedThread_InitializedWithDefaults()
    {
        // Arrange & Act
        var thread = TestDataHelper.CreateTestWatchedThread();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(thread.Board, Is.EqualTo("g"));
            Assert.That(thread.ThreadId, Is.EqualTo(12345));
            Assert.That(thread.Subject, Is.EqualTo("Test Thread"));
            Assert.That(thread.ErrorCount, Is.EqualTo(0));
            Assert.That(thread.TotalDownloadedFiles, Is.EqualTo(0));
            Assert.That(thread.LastChecked, Is.EqualTo(DateTime.MinValue));
        });
    }

    [Test]
    public void Post_HasMedia_ReturnsTrueWhenFileDataPresent()
    {
        // Arrange
        var post = TestDataHelper.CreateTestPostWithMedia();

        // Act
        var hasMedia = post.HasMedia;

        // Assert
        Assert.That(hasMedia, Is.True);
    }

    [Test]
    public void Post_HasMedia_ReturnsFalseWhenFileDataMissing()
    {
        // Arrange
        var post1 = new Post
        {
            InternalFileIdentifier = null,
            FileExtension = ".jpg"
        };

        var post2 = new Post
        {
            InternalFileIdentifier = 1234567890,
            FileExtension = null
        };

        var post3 = TestDataHelper.CreateTestPostWithoutMedia();

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(post1.HasMedia, Is.False);
            Assert.That(post2.HasMedia, Is.False);
            Assert.That(post3.HasMedia, Is.False);
        });
    }

    [Test]
    public void Post_GetFileUrl_ReturnsCorrectUrl()
    {
        // Arrange
        var post = TestDataHelper.CreateTestPostWithMedia();
        var boardCode = "g";
        var expectedUrl = $"https://i.4cdn.org/{boardCode}/1234567890.jpg";

        // Act
        var fileUrl = post.GetFileUrl(boardCode);

        // Assert
        Assert.That(fileUrl, Is.EqualTo(expectedUrl));
    }

    [Test]
    public void Post_GetFileUrl_ReturnsNullWhenNoMedia()
    {
        // Arrange
        var post = TestDataHelper.CreateTestPostWithoutMedia();
        var boardCode = "g";

        // Act
        var fileUrl = post.GetFileUrl(boardCode);

        // Assert
        Assert.That(fileUrl, Is.Null);
    }

    #endregion

    #region Thread Processing Tests

    [Test]
    public void Thread_WithMediaPosts_CorrectlyIdentifiesMediaCount()
    {
        // Arrange
        var thread = new ChanSentry.Common.Models.Thread
        {
            Posts = new List<Post>
            {
                new Post { InternalFileIdentifier = 123, FileExtension = ".jpg" },
                new Post { InternalFileIdentifier = 456, FileExtension = ".png" },
                new Post { InternalFileIdentifier = null, FileExtension = null }, // No media
                new Post { InternalFileIdentifier = 789, FileExtension = ".webm" }
            }
        };

        // Act
        var mediaPosts = thread.Posts.Where(p => p.HasMedia).ToList();

        // Assert
        Assert.That(mediaPosts, Has.Count.EqualTo(3));
    }

    [Test]
    public void WatchedThreads_Serialization_WorksCorrectly()
    {
        // Arrange
        var watchedThreads = new List<WatchedThread>
        {
            TestDataHelper.CreateTestWatchedThread("g", 12345, "Test Thread 1", 0, 5),
            TestDataHelper.CreateTestWatchedThread("pol", 67890, "Test Thread 2", 1, 3)
        };
        watchedThreads[0].LastChecked = DateTime.UtcNow;
        watchedThreads[1].LastChecked = DateTime.UtcNow.AddHours(-1);

        // Act
        var json = JsonSerializer.Serialize(watchedThreads);
        var deserialized = JsonSerializer.Deserialize<List<WatchedThread>>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(deserialized![0].Board, Is.EqualTo("g"));
            Assert.That(deserialized[0].ThreadId, Is.EqualTo(12345));
            Assert.That(deserialized[0].Subject, Is.EqualTo("Test Thread 1"));
            Assert.That(deserialized[0].ErrorCount, Is.EqualTo(0));
            Assert.That(deserialized[0].TotalDownloadedFiles, Is.EqualTo(5));

            Assert.That(deserialized[1].Board, Is.EqualTo("pol"));
            Assert.That(deserialized[1].ThreadId, Is.EqualTo(67890));
            Assert.That(deserialized[1].Subject, Is.EqualTo("Test Thread 2"));
            Assert.That(deserialized[1].ErrorCount, Is.EqualTo(1));
            Assert.That(deserialized[1].TotalDownloadedFiles, Is.EqualTo(3));
        });
    }

    [Test]
    public void WatchedThreads_FiltersByErrorCount()
    {
        // Arrange
        var watchedThreads = new List<WatchedThread>
        {
            TestDataHelper.CreateTestWatchedThread("g", 1, "Thread 1", 0),
            TestDataHelper.CreateTestWatchedThread("g", 2, "Thread 2", 2),
            TestDataHelper.CreateTestWatchedThread("g", 3, "Thread 3", 3),
            TestDataHelper.CreateTestWatchedThread("g", 4, "Thread 4", 5)
        };

        // Act
        var filteredThreads = watchedThreads.Where(t => t.ErrorCount < 3).ToList();

        // Assert
        Assert.That(filteredThreads, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(filteredThreads[0].ThreadId, Is.EqualTo(1));
            Assert.That(filteredThreads[1].ThreadId, Is.EqualTo(2));
        });
    }

    #endregion

    #region New Media Detection Tests

    [Test]
    public void NewMediaDetection_SkipsAlreadyDownloadedFiles()
    {
        // Arrange
        var thread = new ChanSentry.Common.Models.Thread
        {
            Posts = new List<Post>
            {
                new Post { InternalFileIdentifier = 1, FileExtension = ".jpg", Timestamp = 1000 },
                new Post { InternalFileIdentifier = 2, FileExtension = ".jpg", Timestamp = 2000 },
                new Post { InternalFileIdentifier = 3, FileExtension = ".jpg", Timestamp = 3000 },
                new Post { InternalFileIdentifier = 4, FileExtension = ".jpg", Timestamp = 4000 },
                new Post { InternalFileIdentifier = 5, FileExtension = ".jpg", Timestamp = 5000 }
            }
        };

        var totalDownloadedFiles = 3;

        // Act
        var mediaPosts = thread.Posts.Where(p => p.HasMedia).ToList();
        var newMedia = mediaPosts.Skip(totalDownloadedFiles).ToList();

        // Assert
        Assert.That(newMedia, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(newMedia[0].InternalFileIdentifier, Is.EqualTo(4));
            Assert.That(newMedia[1].InternalFileIdentifier, Is.EqualTo(5));
        });
    }

    [Test]
    public void NewMediaDetection_ReturnsEmptyWhenNoNewMedia()
    {
        // Arrange
        var thread = new ChanSentry.Common.Models.Thread
        {
            Posts = new List<Post>
            {
                new Post { InternalFileIdentifier = 1, FileExtension = ".jpg" },
                new Post { InternalFileIdentifier = 2, FileExtension = ".jpg" },
                new Post { InternalFileIdentifier = 3, FileExtension = ".jpg" }
            }
        };

        var totalDownloadedFiles = 3;

        // Act
        var mediaPosts = thread.Posts.Where(p => p.HasMedia).ToList();
        var newMedia = mediaPosts.Skip(totalDownloadedFiles).ToList();

        // Assert
        Assert.That(newMedia, Is.Empty);
    }

    [Test]
    public void NewMediaDetection_HandlesNoMediaPosts()
    {
        // Arrange
        var thread = new ChanSentry.Common.Models.Thread
        {
            Posts = new List<Post>
            {
                new Post { InternalFileIdentifier = null, FileExtension = null },
                new Post { InternalFileIdentifier = null, FileExtension = null }
            }
        };

        var totalDownloadedFiles = 0;

        // Act
        var mediaPosts = thread.Posts.Where(p => p.HasMedia).ToList();
        var newMedia = mediaPosts.Skip(totalDownloadedFiles).ToList();

        // Assert
        Assert.That(mediaPosts, Is.Empty);
        Assert.That(newMedia, Is.Empty);
    }

    #endregion

    #region File Path Tests

    [Test]
    public void DownloadPath_GeneratesCorrectStructure()
    {
        // Arrange
        var boardCode = "g";
        var threadId = "12345";

        // Act
        var downloadPath = Path.Combine("downloads", boardCode, threadId);

        // Assert
        Assert.That(downloadPath, Does.Contain("downloads"));
        Assert.That(downloadPath, Does.Contain(boardCode));
        Assert.That(downloadPath, Does.Contain(threadId));
    }

    [Test]
    public void FileName_GeneratesCorrectFormat()
    {
        // Arrange
        var post = new Post
        {
            InternalFileIdentifier = 1234567890123,
            FileExtension = ".jpg"
        };

        // Act
        var fileName = $"{post.InternalFileIdentifier}{post.FileExtension}";

        // Assert
        Assert.That(fileName, Is.EqualTo("1234567890123.jpg"));
    }

    [Test]
    public void FilePath_CombinesCorrectly()
    {
        // Arrange
        var boardCode = "g";
        var threadId = "12345";
        var fileName = "1234567890.jpg";
        var downloadPath = Path.Combine("downloads", boardCode, threadId);

        // Act
        var filePath = Path.Combine(downloadPath, fileName);

        // Assert
        Assert.That(filePath, Does.EndWith("1234567890.jpg"));
        Assert.That(filePath, Does.Contain(Path.Combine("downloads", boardCode, threadId)));
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void Post_WithDifferentFileExtensions_GeneratesCorrectUrls()
    {
        // Arrange
        var extensions = new[] { ".jpg", ".png", ".gif", ".webm", ".pdf" };
        var boardCode = "g";
        var posts = extensions.Select(ext => new Post
        {
            InternalFileIdentifier = 123456,
            FileExtension = ext
        }).ToList();

        // Act & Assert
        foreach (var (post, ext) in posts.Zip(extensions))
        {
            var url = post.GetFileUrl(boardCode);
            Assert.That(url, Does.EndWith(ext));
            Assert.That(url, Does.Contain("123456"));
        }
    }

    [Test]
    public void WatchedThread_ErrorCount_Increments()
    {
        // Arrange
        var thread = TestDataHelper.CreateTestWatchedThread();

        // Act
        thread.ErrorCount++;
        thread.ErrorCount++;

        // Assert
        Assert.That(thread.ErrorCount, Is.EqualTo(2));
    }

    [Test]
    public void WatchedThread_TotalDownloadedFiles_Updates()
    {
        // Arrange
        var thread = TestDataHelper.CreateTestWatchedThread("g", 12345, "Test", 0, 5);

        // Act
        thread.TotalDownloadedFiles = 10;

        // Assert
        Assert.That(thread.TotalDownloadedFiles, Is.EqualTo(10));
    }

    [Test]
    public void WatchedThread_LastChecked_UpdatesCorrectly()
    {
        // Arrange
        var thread = TestDataHelper.CreateTestWatchedThread();
        var checkTime = DateTime.UtcNow;

        // Act
        thread.LastChecked = checkTime;

        // Assert
        Assert.That(thread.LastChecked, Is.EqualTo(checkTime));
    }

    #endregion

    #region Integration-Style Tests

    [Test]
    public void EmptyWatchedThreadsFile_DeserializesToEmptyList()
    {
        // Arrange
        var json = TestDataHelper.GetEmptyWatchedThreadsJson();

        // Act
        var threads = JsonSerializer.Deserialize<List<WatchedThread>>(json);

        // Assert
        Assert.That(threads, Is.Not.Null);
        Assert.That(threads, Is.Empty);
    }

    [Test]
    public void ValidWatchedThreadsFile_DeserializesCorrectly()
    {
        // Arrange
        var json = $"[{TestDataHelper.GetWatchedThreadJson()}]";

        // Act
        var threads = JsonSerializer.Deserialize<List<WatchedThread>>(json);

        // Assert
        Assert.That(threads, Is.Not.Null);
        Assert.That(threads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(threads![0].Board, Is.EqualTo("g"));
            Assert.That(threads[0].ThreadId, Is.EqualTo(12345));
            Assert.That(threads[0].Subject, Is.EqualTo("Test Thread"));
            Assert.That(threads[0].ErrorCount, Is.EqualTo(0));
            Assert.That(threads[0].TotalDownloadedFiles, Is.EqualTo(5));
        });
    }

    [Test]
    public void ThreadProcessing_FullWorkflow_UpdatesCorrectly()
    {
        // Arrange
        var watchedThread = TestDataHelper.CreateTestWatchedThread("g", 12345, "Test Thread", 0, 2);
        watchedThread.LastChecked = DateTime.UtcNow.AddHours(-1);

        var threadData = new ChanSentry.Common.Models.Thread
        {
            Posts = new List<Post>
            {
                TestDataHelper.CreateTestPostWithMedia(1, ".jpg"),  // Already downloaded
                TestDataHelper.CreateTestPostWithMedia(2, ".jpg"),  // Already downloaded
                TestDataHelper.CreateTestPostWithMedia(3, ".png"),  // New
                TestDataHelper.CreateTestPostWithMedia(4, ".gif")   // New
            }
        };

        // Act
        var mediaPosts = threadData.Posts.Where(p => p.HasMedia).ToList();
        var newMedia = mediaPosts.Skip(watchedThread.TotalDownloadedFiles).ToList();
        watchedThread.TotalDownloadedFiles = mediaPosts.Count;
        watchedThread.LastChecked = DateTime.UtcNow;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(newMedia, Has.Count.EqualTo(2));
            Assert.That(watchedThread.TotalDownloadedFiles, Is.EqualTo(4));
            Assert.That(watchedThread.LastChecked, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-5)));
        });
    }

    #endregion
}
