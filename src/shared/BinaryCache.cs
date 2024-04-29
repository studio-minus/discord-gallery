using System.IO.Hashing;
using System.Reflection.Metadata;
using System.Text;

namespace gallery.shared;

public static class BinaryCache
{
    private static readonly PersistentCollection<Entry> entries = new("tmp_binarycachering");

    public static async Task<string> Store(string originalPath)
    {
        using HttpClient downloader = new();
        using var data = await downloader.GetAsync(originalPath);
        using var stream = await data.Content.ReadAsStreamAsync();
        var outputPath = GetCachedPathFor(originalPath);
        using var file = new FileStream(outputPath, FileMode.Create);
        await stream.CopyToAsync(file);

        entries.Add(new()
        {
            CreatedAt = DateTime.UtcNow,
            Path = file.Name
        });

        DeleteTooOld();

        return outputPath;
    }

    private static void DeleteTooOld()
    {
        var now = DateTime.UtcNow;
        var ageThreshold = TimeSpan.FromDays(14);

        foreach (var item in entries)
        {
            if (item.Path == null)
                continue;
            var age = now - item.CreatedAt;
            if (age >= ageThreshold || !File.Exists(item.Path))
            {
                try
                {
                    File.Delete(item.Path);
                }
                catch (Exception w)
                {
                    Console.Error.WriteLine(w);
                }
                item.Path = null;
            }
        }

        entries.Remove(e => e.Path == null);
    }

    public static string GetCachedPathFor(string input)
    {
        return Path.Combine(Configuration.Current.CachePath, XxHash3.HashToUInt64(Encoding.UTF8.GetBytes(input.ToLowerInvariant())).ToString("x2")).Replace('\\', '/');
    }

    public static void Initialise()
    {
        _ = entries.Count;
    }

    public class Entry
    {
        public string? Path;
        public DateTime CreatedAt;
    }
}