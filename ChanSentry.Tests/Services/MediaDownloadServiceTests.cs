using ChanSentry.CLI.Services;
using ChanSentry.Common.Models;

namespace ChanSentry.Tests.Services;

[TestFixture]
public class MediaDownloadServiceTests
{
    private string _testDirectory = string.Empty;
    private string _originalDirectory = string.Empty;
    private MediaDownloadService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ChanSentryTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        Directory.SetCurrentDirectory(_testDirectory);
        
        _service = new MediaDownloadService();
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            // Restore original directory first
            Directory.SetCurrentDirectory(_originalDirectory);
        }
        catch
        {
            // If original directory doesn't exist, set to temp
            Directory.SetCurrentDirectory(Path.GetTempPath());
        }
        
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Download Directory Tests

    [Test]
    public async Task DownloadMediaFilesAsync_CreatesDownloadDirectory()
    {
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = "Test Subject"
        };
        var posts = new List<Post>();

        await _service.DownloadMediaFilesAsync(posts, thread);

        var expectedPath = Path.Combine(_testDirectory, "downloads", "g", "12345 - Test Subject");
        Assert.That(Directory.Exists(expectedPath), Is.True);
    }

    [Test]
    public async Task DownloadMediaFilesAsync_WithEmptySubject_CreatesDirectoryWithThreadIdOnly()
    {
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = ""
        };
        var posts = new List<Post>();

        await _service.DownloadMediaFilesAsync(posts, thread);

        var expectedPath = Path.Combine(_testDirectory, "downloads", "g", "12345");
        Assert.That(Directory.Exists(expectedPath), Is.True);
    }

    [Test]
    public async Task DownloadMediaFilesAsync_WithNullSubject_CreatesDirectoryWithThreadIdOnly()
    {
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = null!
        };
        var posts = new List<Post>();

        await _service.DownloadMediaFilesAsync(posts, thread);

        var expectedPath = Path.Combine(_testDirectory, "downloads", "g", "12345");
        Assert.That(Directory.Exists(expectedPath), Is.True);
    }

    [Test]
    public async Task DownloadMediaFilesAsync_WithInvalidCharactersInSubject_SanitizesDirectoryName()
    {
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = "Test<>:|?*Subject"
        };
        var posts = new List<Post>();

        await _service.DownloadMediaFilesAsync(posts, thread);

        // The sanitizer replaces invalid characters with underscores
        // Each invalid character becomes one underscore
        var expectedPath = Path.Combine(_testDirectory, "downloads", "g", "12345 - Test______Subject");
        Assert.That(Directory.Exists(expectedPath), Is.True);
    }

    #endregion

    #region Folder Renaming Tests

    [Test]
    public async Task DownloadMediaFilesAsync_RenamesOldFolderToIncludeSubject()
    {
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = "New Subject"
        };
        
        var oldPath = Path.Combine(_testDirectory, "downloads", "g", "12345");
        Directory.CreateDirectory(oldPath);
        
        File.WriteAllText(Path.Combine(oldPath, "test.txt"), "content");

        var posts = new List<Post>();
        await _service.DownloadMediaFilesAsync(posts, thread);

        var newPath = Path.Combine(_testDirectory, "downloads", "g", "12345 - New Subject");
        
        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(oldPath), Is.False);
            Assert.That(Directory.Exists(newPath), Is.True);
            Assert.That(File.Exists(Path.Combine(newPath, "test.txt")), Is.True);
        });
    }

    [Test]
    public async Task DownloadMediaFilesAsync_DoesNotRenameIfOldFolderDoesNotExist()
    {
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = "Subject"
        };
        var posts = new List<Post>();

        await _service.DownloadMediaFilesAsync(posts, thread);

        var expectedPath = Path.Combine(_testDirectory, "downloads", "g", "12345 - Subject");
        Assert.That(Directory.Exists(expectedPath), Is.True);
    }

    [Test]
    public async Task DownloadMediaFilesAsync_DoesNotRenameIfPathsAreSame()
    {
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = ""
        };
        
        var path = Path.Combine(_testDirectory, "downloads", "g", "12345");
        Directory.CreateDirectory(path);

        var posts = new List<Post>();
        await _service.DownloadMediaFilesAsync(posts, thread);

        Assert.That(Directory.Exists(path), Is.True);
    }

    #endregion

    #region File Name Tests

    [Test]
    public async Task DownloadMediaFilesAsync_WithEmptyPostList_DoesNotDownloadAnything()
    {
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = "Test"
        };
        var posts = new List<Post>();

        await _service.DownloadMediaFilesAsync(posts, thread);

        var downloadPath = Path.Combine(_testDirectory, "downloads", "g", "12345 - Test");
        var files = Directory.GetFiles(downloadPath);
        
        Assert.That(files, Is.Empty);
    }

    [Test]
    public async Task DownloadMediaFilesAsync_WithPostWithoutMedia_SkipsDownload()
    {
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = "Test"
        };
        var posts = new List<Post>
        {
            new Post
            {
                InternalFileIdentifier = null,
                FileExtension = null
            }
        };

        await _service.DownloadMediaFilesAsync(posts, thread);

        var downloadPath = Path.Combine(_testDirectory, "downloads", "g", "12345 - Test");
        var files = Directory.GetFiles(downloadPath);
        
        Assert.That(files, Is.Empty);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task DownloadMediaFilesAsync_WithVeryLongSubject_TruncatesTo200Characters()
    {
        var longSubject = new string('a', 300);
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = longSubject
        };
        var posts = new List<Post>();

        await _service.DownloadMediaFilesAsync(posts, thread);

        var truncatedSubject = new string('a', 200);
        var expectedPath = Path.Combine(_testDirectory, "downloads", "g", $"12345 - {truncatedSubject}");
        
        Assert.That(Directory.Exists(expectedPath), Is.True);
    }

    [Test]
    public async Task DownloadMediaFilesAsync_WithDifferentBoards_CreatesCorrectPaths()
    {
        var boards = new[] { "g", "pol", "b", "fit" };
        
        foreach (var board in boards)
        {
            var thread = new WatchedThread
            {
                Board = board,
                ThreadId = 12345,
                Subject = "Test"
            };
            var posts = new List<Post>();

            await _service.DownloadMediaFilesAsync(posts, thread);

            var expectedPath = Path.Combine(_testDirectory, "downloads", board, "12345 - Test");
            Assert.That(Directory.Exists(expectedPath), Is.True, $"Path for board {board} should exist");
        }
    }

    [Test]
    public async Task DownloadMediaFilesAsync_WithWhitespaceOnlySubject_TreatsAsEmpty()
    {
        var thread = new WatchedThread
        {
            Board = "g",
            ThreadId = 12345,
            Subject = "   "
        };
        var posts = new List<Post>();

        await _service.DownloadMediaFilesAsync(posts, thread);

        var expectedPath = Path.Combine(_testDirectory, "downloads", "g", "12345");
        Assert.That(Directory.Exists(expectedPath), Is.True);
    }

    #endregion

    #region Integration Notes

    [Test]
    public void DownloadMediaFilesAsync_HttpIntegration_Note()
    {
        // Note: Full integration tests for actual HTTP downloads would require:
        // 1. Mocking HttpClient
        // 2. Test HTTP server
        // 3. Or dependency injection of HttpClient
        
        Assert.Pass("HTTP download tests require mocking. Directory management tests are complete.");
    }

    #endregion
}
