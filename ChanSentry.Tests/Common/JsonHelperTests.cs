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

            // Act
            var result = JsonHelper.Deserialize<Board>(json);

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

            // Act
            var result = JsonHelper.Deserialize<Boards>(json);

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

            // Act
            var result = JsonHelper.Deserialize<ChanSentry.Common.Models.Thread>(json);

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

            // Act
            var result = JsonHelper.Deserialize<Post>(json);

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

            // Act
            var result = JsonHelper.Deserialize<Post>(json);

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

            // Act
            var result = JsonHelper.Deserialize<CatalogThread>(json);

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

            // Act
            var result = JsonHelper.Deserialize<CatalogThreads>(json);

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

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonHelper.Deserialize<Board>(json));
        }

        [Test]
        public void Deserialize_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var json = TestDataHelper.GetInvalidJson();

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonHelper.Deserialize<Board>(json));
        }

        [Test]
        public void Deserialize_NullJson_ThrowsArgumentNullException()
        {
            // Arrange
            string? json = TestDataHelper.GetNullJson();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => JsonHelper.Deserialize<Board>(json!));
        }

        [Test]
        public void Deserialize_EmptyJsonObject_ReturnsObjectWithDefaultValues()
        {
            // Arrange
            var json = TestDataHelper.GetEmptyJson();

            // Act
            var result = JsonHelper.Deserialize<Board>(json);

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

            // Act
            var result = JsonHelper.Deserialize<Board>(json);

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

            // Act
            var result = JsonHelper.Deserialize<Board>(json);

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
