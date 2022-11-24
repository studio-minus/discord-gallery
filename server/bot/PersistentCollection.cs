using Ceras;
using System.Collections;

namespace bot;

public class PersistentCollection<T> : IDisposable, IEnumerable<T>
{
    public readonly FileInfo Source;

    private List<T> coll = new();
    private readonly CerasSerializer ceras = new();
    private readonly Mutex mut = new();

    public PersistentCollection(string path)
    {
        Source = new FileInfo(Path.GetFullPath(path));
        LoadFromSource();
    }

    private void LoadFromSource()
    {
        if (File.Exists(Source.FullName))
            ceras.Deserialize(ref coll, File.ReadAllBytes(Source.FullName));
    }

    private void SaveToSource()
    {
        File.WriteAllBytes(Source.FullName, ceras.Serialize(coll));
    }

    public void Add(T t)
    {
        mut.WaitOne();

        try
        {
            coll.Add(t);
            SaveToSource();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            mut.ReleaseMutex();
        }
    }

    public bool Remove(T t)
    {
        mut.WaitOne();
        try
        {
            var b = coll.Remove(t);
            if (b)
                SaveToSource();
            return b;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            mut.ReleaseMutex();
        }
    }

    public bool Remove(Predicate<T> predicate)
    {
        mut.WaitOne();
        try
        {
            bool removed = coll.RemoveAll(predicate) > 0;
            if (removed)
                SaveToSource();
            return removed;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            mut.ReleaseMutex();
        }
    }

    public void Dispose()
    {
        mut.Dispose();
    }

    public IEnumerator<T> GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => coll.AsReadOnly().GetEnumerator();
}