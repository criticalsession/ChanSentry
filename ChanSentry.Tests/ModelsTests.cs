namespace ChanSentry.Tests
{
    public class ModelsTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetFileUrl_WithData_ReturnsCorrectFullFileUrl()
        {
            var t = new Common.Models.Thread()
            {
                Posts = new List<Common.Models.Post>()
                {
                    new Common.Models.Post()
                    {
                        FileExtension = ".png",
                        InternalFileIdentifier = 1745612650141704,
                        FileName = "sticky btfo",
                        Timestamp = 1745612650
                    },
                    new Common.Models.Post()
                    {
                        FileExtension = ".png",
                        InternalFileIdentifier = 1745612666469146,
                        FileName = null,
                        Timestamp = 1745612666
                    },
                    new Common.Models.Post()
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
    }
}
