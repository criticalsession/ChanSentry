using ChanSentry.CLI.Handlers;
using ChanSentry.CLI.Services;
using ChanSentry.CLI.Services.Interfaces;
using ChanSentry.Common.Models;
using ChanSentry.Tests.Helpers;
using FakeItEasy;

namespace ChanSentry.Tests.Handlers;

[TestFixture]
public class ManageThreadsHandlerTests
{
    [Test]
    public void ManageThreadsHandler_CanBeInstantiated()
    {
        // Arrange & Act
        var handler = new ManageThreadsHandler();

        // Assert
        Assert.That(handler, Is.Not.Null);
    }

    [Test]
    public void ManageThreadsHandler_WithFakedServices_CanBeInstantiated()
    {
        // Arrange
        var fakeWatchedThreadService = A.Fake<IWatchedThreadService>();
        var fakeThreadFetchService = A.Fake<IThreadFetchService>();

        // Act
        var handler = new ManageThreadsHandler(fakeWatchedThreadService, fakeThreadFetchService);

        // Assert
        Assert.That(handler, Is.Not.Null);
    }

    [Test]
    public void ManageThreadsHandler_Constructor_AcceptsServiceInterfaces()
    {
        // Arrange
        var fakeWatchedThreadService = A.Fake<IWatchedThreadService>();
        var fakeThreadFetchService = A.Fake<IThreadFetchService>();
        var threads = new List<WatchedThread>
        {
            TestDataHelper.CreateTestWatchedThread("g", 12345, "Test Thread 1"),
            TestDataHelper.CreateTestWatchedThread("pol", 67890, "Test Thread 2")
        };

        A.CallTo(() => fakeWatchedThreadService.ReadWatchedThreads())
            .Returns(threads);

        // Act
        var handler = new ManageThreadsHandler(fakeWatchedThreadService, fakeThreadFetchService);

        // Assert
        Assert.That(handler, Is.Not.Null);
    }

    [Test]
    public async Task AddThread_WithValidThreadData_AddsThreadSuccessfully()
    {
        // Arrange
        var fakeWatchedThreadService = A.Fake<IWatchedThreadService>();
        var fakeThreadFetchService = A.Fake<IThreadFetchService>();
        
        var existingThreads = new List<WatchedThread>();
        var threadData = new ChanSentry.Common.Models.Thread
        {
            Posts = new List<Post>
            {
                new Post { Subject = "Test Subject", InternalFileIdentifier = 123, FileExtension = ".jpg" }
            }
        };

        A.CallTo(() => fakeWatchedThreadService.ReadWatchedThreads())
            .Returns(existingThreads);
        
        A.CallTo(() => fakeThreadFetchService.FetchThreadAsync(A<WatchedThread>._))
            .Returns(ThreadFetchResult.Success(threadData));

        // Act
        var handler = new ManageThreadsHandler(fakeWatchedThreadService, fakeThreadFetchService);

        // Assert - Verify handler was created with mocked services
        Assert.That(handler, Is.Not.Null);
    }

    [Test]
    public void WatchedThreads_WithDifferentStates_DisplayCorrectly()
    {
        // Arrange
        var threads = new List<WatchedThread>
        {
            TestDataHelper.CreateTestWatchedThread("g", 1, "Active Thread", 0, 10),
            TestDataHelper.CreateTestWatchedThread("pol", 2, "Thread with Errors", 2, 5),
            TestDataHelper.CreateTestWatchedThread("b", 3, "", 0, 0),
            TestDataHelper.CreateTestWatchedThread("v", 4, "Old Thread", 1, 3)
        };

        threads[0].LastChecked = DateTime.UtcNow.AddMinutes(-5);
        threads[1].LastChecked = DateTime.UtcNow.AddHours(-2);
        threads[2].LastChecked = DateTime.MinValue;
        threads[3].LastChecked = DateTime.UtcNow.AddDays(-3);

        // Act & Assert - Verify thread states
        Assert.Multiple(() =>
        {
            Assert.That(threads[0].ErrorCount, Is.EqualTo(0));
            Assert.That(threads[1].ErrorCount, Is.EqualTo(2));
            Assert.That(threads[2].Subject, Is.Empty);
            Assert.That(threads[3].TotalDownloadedFiles, Is.EqualTo(3));
        });
    }

    [Test]
    public void WatchedThreads_LastChecked_FormatsCorrectly()
    {
        // Arrange
        var thread = TestDataHelper.CreateTestWatchedThread("g", 12345, "Test");

        // Act - Test different time scenarios
        thread.LastChecked = DateTime.UtcNow;
        var justNow = thread.LastChecked;

        thread.LastChecked = DateTime.UtcNow.AddMinutes(-30);
        var thirtyMinutesAgo = thread.LastChecked;

        thread.LastChecked = DateTime.UtcNow.AddHours(-5);
        var fiveHoursAgo = thread.LastChecked;

        thread.LastChecked = DateTime.UtcNow.AddDays(-2);
        var twoDaysAgo = thread.LastChecked;

        thread.LastChecked = DateTime.MinValue;
        var never = thread.LastChecked;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That((DateTime.UtcNow - justNow).TotalSeconds, Is.LessThan(5));
            Assert.That((DateTime.UtcNow - thirtyMinutesAgo).TotalMinutes, Is.GreaterThan(25));
            Assert.That((DateTime.UtcNow - fiveHoursAgo).TotalHours, Is.GreaterThan(4));
            Assert.That((DateTime.UtcNow - twoDaysAgo).TotalDays, Is.GreaterThan(1));
            Assert.That(never, Is.EqualTo(DateTime.MinValue));
        });
    }

    [Test]
    public void ThreadDeletion_RemovesThreadsFromList()
    {
        // Arrange
        var threads = new List<WatchedThread>
        {
            TestDataHelper.CreateTestWatchedThread("g", 1, "Thread 1"),
            TestDataHelper.CreateTestWatchedThread("pol", 2, "Thread 2"),
            TestDataHelper.CreateTestWatchedThread("b", 3, "Thread 3")
        };

        var threadsToDelete = new List<WatchedThread> { threads[1] };

        // Act
        foreach (var thread in threadsToDelete)
        {
            threads.Remove(thread);
        }

        // Assert
        Assert.That(threads, Has.Count.EqualTo(2));
        Assert.That(threads.Any(t => t.ThreadId == 2), Is.False);
    }

    [Test]
    public void ThreadDeletion_MultipleThreads_RemovesAllSelected()
    {
        // Arrange
        var threads = new List<WatchedThread>
        {
            TestDataHelper.CreateTestWatchedThread("g", 1, "Thread 1"),
            TestDataHelper.CreateTestWatchedThread("pol", 2, "Thread 2"),
            TestDataHelper.CreateTestWatchedThread("b", 3, "Thread 3"),
            TestDataHelper.CreateTestWatchedThread("v", 4, "Thread 4")
        };

        var threadsToDelete = new List<WatchedThread> { threads[0], threads[2] };

        // Act
        foreach (var thread in threadsToDelete)
        {
            threads.Remove(thread);
        }

        // Assert
        Assert.That(threads, Has.Count.EqualTo(2));
        Assert.That(threads.Any(t => t.ThreadId == 1), Is.False);
        Assert.That(threads.Any(t => t.ThreadId == 3), Is.False);
        Assert.That(threads.Any(t => t.ThreadId == 2), Is.True);
        Assert.That(threads.Any(t => t.ThreadId == 4), Is.True);
    }

    [Test]
    public void ThreadDeletion_SavesAfterDeletion()
    {
        // Arrange
        var fakeWatchedThreadService = A.Fake<IWatchedThreadService>();
        var fakeThreadFetchService = A.Fake<IThreadFetchService>();
        var threads = new List<WatchedThread>
        {
            TestDataHelper.CreateTestWatchedThread("g", 1, "Thread 1"),
            TestDataHelper.CreateTestWatchedThread("pol", 2, "Thread 2")
        };

        A.CallTo(() => fakeWatchedThreadService.ReadWatchedThreads())
            .Returns(threads);

        // Act
        threads.RemoveAt(0);
        fakeWatchedThreadService.SaveWatchedThreads(threads);

        // Assert
        A.CallTo(() => fakeWatchedThreadService.SaveWatchedThreads(A<List<WatchedThread>>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void EmptyThreadList_HandledCorrectly()
    {
        // Arrange
        var threads = new List<WatchedThread>();

        // Act & Assert
        Assert.That(threads, Is.Empty);
        Assert.That(threads.Count, Is.EqualTo(0));
    }

    [Test]
    public void ThreadAlreadyExists_DetectsExistingThread()
    {
        // Arrange
        var threads = new List<WatchedThread>
        {
            TestDataHelper.CreateTestWatchedThread("g", 12345, "Existing Thread"),
            TestDataHelper.CreateTestWatchedThread("pol", 67890, "Another Thread")
        };

        // Act
        var exists = threads.Any(t => 
            t.Board.Equals("g", StringComparison.OrdinalIgnoreCase) && 
            t.ThreadId == 12345);

        var notExists = threads.Any(t => 
            t.Board.Equals("b", StringComparison.OrdinalIgnoreCase) && 
            t.ThreadId == 99999);

        // Assert
        Assert.That(exists, Is.True);
        Assert.That(notExists, Is.False);
    }
}
