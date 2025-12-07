using System.Text.Json;
using ChanSentry.Common.Helpers;
using ChanSentry.Common.Models;
using ChanSentry.Tests.Helpers;

namespace ChanSentry.Tests.Common
{
    [TestFixture]
    public class ModelsTests
    {
        #region Board Tests

        [Test]
        public void Board_DeserializeFromJson_CorrectlyMapsProperties()
        {
            // Arrange
            var json = TestDataHelper.GetSingleBoardJson();

            // Act
            var board = JsonSerializer.Deserialize<Board>(json);

            // Assert
            Assert.That(board, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(board.BoardCode, Is.EqualTo("g"));
                Assert.That(board.Title, Is.EqualTo("Technology"));
                Assert.That(board.IsWorkSafe, Is.EqualTo(1));
                Assert.That(board.Description, Is.EqualTo("Discussion of technology and related topics."));
            });
        }

        [Test]
        public void Boards_DeserializeFromJson_CorrectlyMapsBoardsList()
        {
            // Arrange
            var json = TestDataHelper.GetMultipleBoardsJson();

            // Act
            var boards = JsonSerializer.Deserialize<Boards>(json);

            // Assert
            Assert.That(boards, Is.Not.Null);
            Assert.That(boards.BoardsList, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(boards.BoardsList[0].BoardCode, Is.EqualTo("g"));
                Assert.That(boards.BoardsList[0].Title, Is.EqualTo("Technology"));
                Assert.That(boards.BoardsList[1].BoardCode, Is.EqualTo("pol"));
                Assert.That(boards.BoardsList[1].IsWorkSafe, Is.EqualTo(0));
            });
        }
        #endregion

        #region Catalog Tests

        [Test]
        public void CatalogThread_DeserializeFromJson_CorrectlyMapsProperties()
        {
            // Arrange
            var json = TestDataHelper.GetSingleCatalogThreadJson();

            // Act
            var catalogThread = JsonSerializer.Deserialize<CatalogThread>(json);

            // Assert
            Assert.That(catalogThread, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(catalogThread.ThreadId, Is.EqualTo(98765432));
                Assert.That(catalogThread.Subject, Is.EqualTo("Test Thread"));
                Assert.That(catalogThread.Comment, Is.EqualTo("This is a test comment"));
                Assert.That(catalogThread.ReplyCount, Is.EqualTo(42));
                Assert.That(catalogThread.ImageCount, Is.EqualTo(10));
            });
        }

        [Test]
        public void CatalogThreads_DeserializeFromJson_CorrectlyMapsThreadList()
        {
            // Arrange
            var json = TestDataHelper.GetMultipleCatalogThreadsJson();

            // Act
            var catalogThreads = JsonSerializer.Deserialize<CatalogThreads>(json);

            // Assert
            Assert.That(catalogThreads, Is.Not.Null);
            Assert.That(catalogThreads.Page, Is.EqualTo(0));
            Assert.That(catalogThreads.ThreadList, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(catalogThreads.ThreadList[0].ThreadId, Is.EqualTo(98765432));
                Assert.That(catalogThreads.ThreadList[0].Subject, Is.EqualTo("Thread 1"));
                Assert.That(catalogThreads.ThreadList[1].ThreadId, Is.EqualTo(98765433));
                Assert.That(catalogThreads.ThreadList[1].ReplyCount, Is.EqualTo(20));
            });
        }
        #endregion

        #region Thread Tests

        [Test]
        public void Post_DeserializeFromJson_CorrectlyMapsProperties()
        {
            // Arrange
            var json = TestDataHelper.GetPostJsonWithAllFields();

            // Act
            var post = JsonSerializer.Deserialize<Post>(json);

            // Assert
            Assert.That(post, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(post.FileName, Is.EqualTo("test_image"));
                Assert.That(post.InternalFileIdentifier, Is.EqualTo(1745612650141704));
                Assert.That(post.FileExtension, Is.EqualTo(".png"));
                Assert.That(post.Timestamp, Is.EqualTo(1745612650));
            });
        }

        [Test]
        public void Post_DeserializeFromJson_WithNullValues_HandlesCorrectly()
        {
            // Arrange
            var json = TestDataHelper.GetPostJsonWithNullableFields();

            // Act
            var post = JsonSerializer.Deserialize<Post>(json);

            // Assert
            Assert.That(post, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(post.FileName, Is.Null);
                Assert.That(post.InternalFileIdentifier, Is.Null);
                Assert.That(post.FileExtension, Is.Null);
                Assert.That(post.Timestamp, Is.EqualTo(1745612650));
            });
        }

        [Test]
        public void Thread_DeserializeFromJson_CorrectlyMapsPostsList()
        {
            // Arrange
            var json = TestDataHelper.GetThreadJsonWithMultiplePosts();

            // Act
            var thread = JsonSerializer.Deserialize<ChanSentry.Common.Models.Thread>(json);

            // Assert
            Assert.That(thread, Is.Not.Null);
            Assert.That(thread.Posts, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(thread.Posts[0].FileName, Is.EqualTo("image1"));
                Assert.That(thread.Posts[0].InternalFileIdentifier, Is.EqualTo(1745612650141704));
                Assert.That(thread.Posts[1].FileName, Is.Null);
                Assert.That(thread.Posts[1].InternalFileIdentifier, Is.Null);
            });
        }

        [Test]
        public void GetFileUrl_WithData_ReturnsCorrectFullFileUrl()
        {
            var t = new ChanSentry.Common.Models.Thread()
            {
                Posts = new List<Post>()
                {
                    new Post()
                    {
                        FileExtension = ".png",
                        InternalFileIdentifier = 1745612650141704,
                        FileName = "sticky btfo",
                        Timestamp = 1745612650
                    },
                    new Post()
                    {
                        FileExtension = ".png",
                        InternalFileIdentifier = 1745612666469146,
                        FileName = null,
                        Timestamp = 1745612666
                    },
                    new Post()
                    {
                        FileExtension = null,
                        InternalFileIdentifier = 1745612680763609,
                        FileName = null,
                        Timestamp = 1745612680
                    }
                },
            };

            Assert.Multiple(() =>
            {
                Assert.That(t.Posts[0].GetFileUrl("g"), Is.EqualTo("https://i.4cdn.org/g/1745612650141704.png"));
                Assert.That(t.Posts[1].GetFileUrl("g"), Is.EqualTo("https://i.4cdn.org/g/1745612666469146.png"));
                Assert.That(t.Posts[2].GetFileUrl("g"), Is.Null);
            });
        }
        #endregion
    }
}
