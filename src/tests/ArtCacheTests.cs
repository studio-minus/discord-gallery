using gallery.front;
using gallery.shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace gallery.tests;

[TestClass]
public class ArtCacheTests
{
    [TestMethod]
    public void BasicOperation()
    {
        // create a new instance of the ArtCache class with a mock art renderer.
        var renderer = new Mock<IArtRenderer>();
        var cache = new ArtCache(renderer.Object);

        // loading an artwork
        var artwork = new Artwork { ImageData = "data:..." };
        var result = cache.Load(100, 100, artwork);
        Assert.IsNotNull(result);

        // clearing the cache
        cache.Clear();
        Assert.AreEqual(0, cache.Count);
    }

    [TestMethod]
    public void NullRenderer()
    {
        var artwork = new Artwork { ImageData = "data:..." };
        var cache = new ArtCache(null);
        Assert.ThrowsExceptionAsync<Exception>(async () => await cache.Load(100, 100, artwork));
    }
}