using ChanSentry.CLI.Services;
using ChanSentry.Common.Models;
using System.Text.Json;

namespace ChanSentry.Tests.Services;

[TestFixture]
public class WatchedThreadServiceTests
{
    private string _testDirectory = string.Empty;
    private string _watchedThreadsFile = string.Empty;
    private WatchedThreadService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ChanSentryTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        _watchedThreadsFile = Path.Combine(_testDirectory, "watched-threads.json");
        
        Directory.SetCurrentDirectory(_testDirectory);
        
        _service = new WatchedThreadService();
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch (IOException)
        {
            // File may be locked, ignore cleanup errors in tests
        }
    }

    #region ReadWatchedThreads Tests

    [Test]
    public void ReadWatchedThreads_WhenFileDoesNotExist_CreatesEmptyFileAndReturnsEmptyList()
    {
        var result = _service.ReadWatchedThreads();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
        Assert.That(File.Exists(_watchedThreadsFile), Is.True);
    }

    [Test]
    public void ReadWatchedThreads_WhenFileExists_ReturnsDeserializedThreads()
    {
        var threads = new List<WatchedThread>
        {
            new WatchedThread { Board = "g", ThreadId = 12345, Subject = "Test Thread 1" },
            new WatchedThread { Board = "pol", ThreadId = 67890, Subject = "Test Thread 2" }
        };
        
        var json = JsonSerializer.Serialize(threads);
        File.WriteAllText(_watchedThreadsFile, json);

        var result = _service.ReadWatchedThreads();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result![0].Board, Is.EqualTo("g"));
            Assert.That(result[0].ThreadId, Is.EqualTo(12345));
            Assert.That(result[0].Subject, Is.EqualTo("Test Thread 1"));
            Assert.That(result[1].Board, Is.EqualTo("pol"));
            Assert.That(result[1].ThreadId, Is.EqualTo(67890));
            Assert.That(result[1].Subject, Is.EqualTo("Test Thread 2"));
        });
    }

    [Test]
    public void ReadWatchedThreads_WhenFileContainsEmptyArray_ReturnsEmptyList()
    {
        File.WriteAllText(_watchedThreadsFile, "[]");

        var result = _service.ReadWatchedThreads();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region SaveWatchedThreads Tests

    [Test]
    public void SaveWatchedThreads_WritesThreadsToFile()
    {
        var threads = new List<WatchedThread>
        {
            new WatchedThread { Board = "g", ThreadId = 12345, Subject = "Test" }
        };

        _service.SaveWatchedThreads(threads);

        Assert.That(File.Exists(_watchedThreadsFile), Is.True);
        
        var json = File.ReadAllText(_watchedThreadsFile);
        var deserialized = JsonSerializer.Deserialize<List<WatchedThread>>(json);
        
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized, Has.Count.EqualTo(1));
        Assert.That(deserialized![0].ThreadId, Is.EqualTo(12345));
    }

    [Test]
    public void SaveWatchedThreads_WithEmptyList_WritesEmptyArray()
    {
        var threads = new List<WatchedThread>();

        _service.SaveWatchedThreads(threads);

        var json = File.ReadAllText(_watchedThreadsFile);
        
        Assert.That(json, Is.EqualTo("[]"));
    }

    [Test]
    public void SaveWatchedThreads_OverwritesExistingFile()
    {
        File.WriteAllText(_watchedThreadsFile, "[{\"Board\":\"old\"}]");
        
        var threads = new List<WatchedThread>
        {
            new WatchedThread { Board = "new", ThreadId = 99999 }
        };

        _service.SaveWatchedThreads(threads);

        var json = File.ReadAllText(_watchedThreadsFile);
        var deserialized = JsonSerializer.Deserialize<List<WatchedThread>>(json);
        
        Assert.That(deserialized![0].Board, Is.EqualTo("new"));
        Assert.That(deserialized[0].ThreadId, Is.EqualTo(99999));
    }

    #endregion

    #region RemoveFailedThreads Tests

    [Test]
    public void RemoveFailedThreads_RemovesThreadsWithThreeOrMoreErrors()
    {
        var threads = new List<WatchedThread>
        {
            new WatchedThread { ThreadId = 1, ErrorCount = 0 },
            new WatchedThread { ThreadId = 2, ErrorCount = 1 },
            new WatchedThread { ThreadId = 3, ErrorCount = 2 },
            new WatchedThread { ThreadId = 4, ErrorCount = 3 },
            new WatchedThread { ThreadId = 5, ErrorCount = 4 }
        };

        var result = _service.RemoveFailedThreads(threads);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].ThreadId, Is.EqualTo(1));
            Assert.That(result[1].ThreadId, Is.EqualTo(2));
            Assert.That(result[2].ThreadId, Is.EqualTo(3));
        });
    }

    [Test]
    public void RemoveFailedThreads_WithNoFailedThreads_ReturnsAllThreads()
    {
        var threads = new List<WatchedThread>
        {
            new WatchedThread { ThreadId = 1, ErrorCount = 0 },
            new WatchedThread { ThreadId = 2, ErrorCount = 1 },
            new WatchedThread { ThreadId = 3, ErrorCount = 2 }
        };

        var result = _service.RemoveFailedThreads(threads);

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void RemoveFailedThreads_WithAllFailedThreads_ReturnsEmptyList()
    {
        var threads = new List<WatchedThread>
        {
            new WatchedThread { ThreadId = 1, ErrorCount = 3 },
            new WatchedThread { ThreadId = 2, ErrorCount = 5 }
        };

        var result = _service.RemoveFailedThreads(threads);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void RemoveFailedThreads_WithEmptyList_ReturnsEmptyList()
    {
        var threads = new List<WatchedThread>();

        var result = _service.RemoveFailedThreads(threads);

        Assert.That(result, Is.Empty);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void ReadAndSave_RoundTrip_PreservesData()
    {
        var originalThreads = new List<WatchedThread>
        {
            new WatchedThread 
            { 
                Board = "g", 
                ThreadId = 12345, 
                Subject = "Test Thread",
                ErrorCount = 1,
                TotalDownloadedFiles = 10,
                LastChecked = DateTime.UtcNow
            }
        };

        _service.SaveWatchedThreads(originalThreads);
        var loadedThreads = _service.ReadWatchedThreads();

        Assert.That(loadedThreads, Is.Not.Null);
        Assert.That(loadedThreads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(loadedThreads![0].Board, Is.EqualTo("g"));
            Assert.That(loadedThreads[0].ThreadId, Is.EqualTo(12345));
            Assert.That(loadedThreads[0].Subject, Is.EqualTo("Test Thread"));
            Assert.That(loadedThreads[0].ErrorCount, Is.EqualTo(1));
            Assert.That(loadedThreads[0].TotalDownloadedFiles, Is.EqualTo(10));
        });
    }

    #endregion
}
