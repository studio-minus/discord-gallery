using gallery.shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace gallery.tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void Serialisation()
    {
        var path = Path.GetTempFileName();

        var config = new Configuration()
        {
            ArtPath = "random",
            FrontPath = "test/nonsense",
            ArtworkFontPath = "/awdw/awd/w.ttf",

            DiscordBotToken = "94d56ad8-73db-11ed-a4c1-ab54ad026903", //bs

            ChannelId = 3022928118123597896u,
            GuildId = 852866005290105344u,

            Ip = "1.2.3.4",
            Port = 29831,

            SslCert = "japan.pfx",
            SslCertPassword = "plaintext lmfao who cares this file should not be accessible by anyone but you"
        };

        File.WriteAllText(path, JsonSerializer.Serialize(config));
        Assert.IsTrue(File.Exists(path));
        Configuration.Load(path);
        Assert.IsNotNull(Configuration.Current);
        Assert.AreEqual(Configuration.Current, config);
        config.ArtPath += "aw";
        Assert.AreNotEqual(Configuration.Current, config);
    }
}
