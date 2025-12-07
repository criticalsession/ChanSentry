using System.Text.Json;
using ChanSentry.Common.Helpers;
using ChanSentry.Common.Models;
using ChanSentry.Tests.Helpers;

namespace ChanSentry.Tests.Common
{
    [TestFixture]
    public class JsonHelperTests
    {
        [Test]
        public void Deserialize_Board_ReturnsCorrectlyDeserializedObject()
        {
            // Arrange
            var json = TestDataHelper.GetSingleBoardJson();
            var board = new Board();

            // Act
            var result = board.Deserialize(json);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.BoardCode, Is.EqualTo("g"));
                Assert.That(result.Title, Is.EqualTo("Technology"));
                Assert.That(result.IsWorkSafe, Is.EqualTo(1));
                Assert.That(result.Description, Is.EqualTo("Discussion of technology and related topics."));
            });
        }

        [Test]
        public void Deserialize_Boards_ReturnsCorrectlyDeserializedList()
        {
            // Arrange
            var json = TestDataHelper.GetMultipleBoardsJson();
            var boards = new Boards();

            // Act
            var result = boards.Deserialize(json);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.BoardsList, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.BoardsList[0].BoardCode, Is.EqualTo("g"));
                Assert.That(result.BoardsList[0].Title, Is.EqualTo("Technology"));
                Assert.That(result.BoardsList[1].BoardCode, Is.EqualTo("pol"));
                Assert.That(result.BoardsList[1].IsWorkSafe, Is.EqualTo(0));
            });
        }

        [Test]
        public void Deserialize_Thread_ReturnsCorrectlyDeserializedPosts()
        {
            // Arrange
            var json = TestDataHelper.GetThreadJsonWithMultiplePosts();
            var thread = new ChanSentry.Common.Models.Thread();

            // Act
            var result = thread.Deserialize(json);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Posts, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Posts[0].FileName, Is.EqualTo("image1"));
                Assert.That(result.Posts[0].InternalFileIdentifier, Is.EqualTo(1745612650141704));
                Assert.That(result.Posts[1].FileName, Is.Null);
                Assert.That(result.Posts[1].InternalFileIdentifier, Is.Null);
            });
        }

        [Test]
        public void Deserialize_Post_WithAllFields_ReturnsCompleteObject()
        {
            // Arrange
            var json = TestDataHelper.GetPostJsonWithAllFields();
            var post = new Post();

            // Act
            var result = post.Deserialize(json);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.FileName, Is.EqualTo("test_image"));
                Assert.That(result.InternalFileIdentifier, Is.EqualTo(1745612650141704));
                Assert.That(result.FileExtension, Is.EqualTo(".png"));
                Assert.That(result.Timestamp, Is.EqualTo(1745612650));
            });
        }

        [Test]
        public void Deserialize_Post_WithNullableFields_HandlesNullsCorrectly()
        {
            // Arrange
            var json = TestDataHelper.GetPostJsonWithNullableFields();
            var post = new Post();

            // Act
            var result = post.Deserialize(json);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.FileName, Is.Null);
                Assert.That(result.InternalFileIdentifier, Is.Null);
                Assert.That(result.FileExtension, Is.Null);
                Assert.That(result.Timestamp, Is.EqualTo(1745612650));
            });
        }

        [Test]
        public void Deserialize_CatalogThread_ReturnsCorrectlyDeserializedObject()
        {
            // Arrange
            var json = TestDataHelper.GetSingleCatalogThreadJson();
            var catalogThread = new CatalogThread();

            // Act
            var result = catalogThread.Deserialize(json);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.ThreadId, Is.EqualTo(98765432));
                Assert.That(result.Subject, Is.EqualTo("Test Thread"));
                Assert.That(result.Comment, Is.EqualTo("This is a test comment"));
                Assert.That(result.ReplyCount, Is.EqualTo(42));
                Assert.That(result.ImageCount, Is.EqualTo(10));
            });
        }

        [Test]
        public void Deserialize_CatalogThreads_ReturnsCorrectlyDeserializedThreadList()
        {
            // Arrange
            var json = TestDataHelper.GetMultipleCatalogThreadsJson();
            var catalogThreads = new CatalogThreads();

            // Act
            var result = catalogThreads.Deserialize(json);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Page, Is.EqualTo(0));
            Assert.That(result.ThreadList, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.ThreadList[0].ThreadId, Is.EqualTo(98765432));
                Assert.That(result.ThreadList[0].Subject, Is.EqualTo("Thread 1"));
                Assert.That(result.ThreadList[1].ThreadId, Is.EqualTo(98765433));
                Assert.That(result.ThreadList[1].ReplyCount, Is.EqualTo(20));
            });
        }

        [Test]
        public void Deserialize_EmptyJson_ThrowsJsonException()
        {
            // Arrange
            var json = TestDataHelper.GetEmptyString();
            var board = new Board();

            // Act & Assert
            Assert.Throws<JsonException>(() => board.Deserialize(json));
        }

        [Test]
        public void Deserialize_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var json = TestDataHelper.GetInvalidJson();
            var board = new Board();

            // Act & Assert
            Assert.Throws<JsonException>(() => board.Deserialize(json));
        }

        [Test]
        public void Deserialize_NullJson_ThrowsArgumentNullException()
        {
            // Arrange
            string? json = TestDataHelper.GetNullJson();
            var board = new Board();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => board.Deserialize(json!));
        }

        [Test]
        public void Deserialize_EmptyJsonObject_ReturnsObjectWithDefaultValues()
        {
            // Arrange
            var json = TestDataHelper.GetEmptyJson();
            var board = new Board();

            // Act
            var result = board.Deserialize(json);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.BoardCode, Is.EqualTo(string.Empty));
                Assert.That(result.Title, Is.EqualTo(string.Empty));
                Assert.That(result.IsWorkSafe, Is.EqualTo(0));
                Assert.That(result.Description, Is.EqualTo(string.Empty));
            });
        }

        [Test]
        public void Deserialize_PartialJson_DeserializesAvailableFieldsOnly()
        {
            // Arrange
            var json = TestDataHelper.GetPartialBoardJson();
            var board = new Board();

            // Act
            var result = board.Deserialize(json);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.BoardCode, Is.EqualTo("g"));
                Assert.That(result.Title, Is.EqualTo("Technology"));
                Assert.That(result.IsWorkSafe, Is.EqualTo(0)); // Default value
                Assert.That(result.Description, Is.EqualTo(string.Empty)); // Default value
            });
        }

        [Test]
        public void Deserialize_JsonWithExtraFields_IgnoresUnknownProperties()
        {
            // Arrange
            var json = TestDataHelper.GetBoardJsonWithExtraFields();
            var board = new Board();

            // Act
            var result = board.Deserialize(json);

            // Assert - Should not throw and should deserialize known fields correctly
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.BoardCode, Is.EqualTo("g"));
                Assert.That(result.Title, Is.EqualTo("Technology"));
                Assert.That(result.IsWorkSafe, Is.EqualTo(1));
                Assert.That(result.Description, Is.EqualTo("Discussion of technology."));
            });
        }
    }
}
