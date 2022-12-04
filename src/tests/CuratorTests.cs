using gallery.bot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace gallery.tests;

[TestClass]
public class CuratorTests
{
    [TestMethod]
    public void Clean()
    {
        const int amount = 15;
        var reference = new List<ArtworkReference>();
        var path = Path.GetTempFileName();
        {
            using var curator = new Curator(path);
            Assert.AreEqual(curator.Count, 0, "New curator already has art");

            for (int i = 0; i < amount; i++)
            {
                var art = new ArtworkReference(
                    (ulong)Random.Shared.NextInt64(),
                    Guid.NewGuid().ToString(),
                    $"https://i.imgur.com/{Random.Shared.Next(10000, 99999)}.jpg");
                reference.Add(art);
                curator.Add(art);
            }
            Assert.AreEqual(curator.Count, amount, "Art was not added");

            for (int i = 0; i < amount / 2; i++)
                curator.IncrementScore(reference[i].MessageId);

            curator.IncrementScore(reference[amount / 3].MessageId); // top
        }
        // away with you

        {
            using var curator = new Curator(path);
            Assert.AreEqual(curator.Count, amount, "Loaded curator should already have art");
            var top = reference[amount / 3];

            var best = curator.GetBestArtwork(5);
            Assert.AreEqual(top.ImageUrl, best.First().ImageData, "Top art does not match");
        }
    }
}