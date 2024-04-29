using Ceras;
using System;
using System.Collections;

namespace gallery.shared;

public class PersistentCollection<T> : IDisposable, IReadOnlyCollection<T>
{
    public readonly FileInfo Source;

    private List<T> coll = [];
    private readonly CerasSerializer ceras = new();
    private readonly Mutex manipulation = new();
    private readonly Mutex saving = new();

    public int Count => coll.Count;

    public PersistentCollection(string path)
    {
        Source = new FileInfo(Path.GetFullPath(path));
        LoadFromSource();
    }

    public void LoadFromSource()
    {
        saving.WaitOne();
        try
        {
            if (File.Exists(Source.FullName))
            {
                var b = File.ReadAllBytes(Source.FullName);
                if (b.Length != 0)
                    ceras.Deserialize(ref coll, b);
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            saving.ReleaseMutex();
        }
    }

    public void SaveToSource()
    {
        saving.WaitOne();
        try
        {
            File.WriteAllBytes(Source.FullName, ceras.Serialize(coll));
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            saving.ReleaseMutex();
        }
    }

    public void Add(T t)
    {
        manipulation.WaitOne();

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
            manipulation.ReleaseMutex();
        }
    }

    public void AddRange(ReadOnlySpan<T> t)
    {
        manipulation.WaitOne();

        try
        {
            foreach (var i in t)
                coll.Add(i);
            SaveToSource();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            manipulation.ReleaseMutex();
        }
    }

    public bool Remove(T t)
    {
        manipulation.WaitOne();
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
            manipulation.ReleaseMutex();
        }
    }

    public bool Remove(Predicate<T> predicate)
    {
        manipulation.WaitOne();
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
            manipulation.ReleaseMutex();
        }
    }

    public void Dispose()
    {
        manipulation.Dispose();
        saving.Dispose();
    }

    public void Clear()
    {
        manipulation.WaitOne();
        try
        {
            coll.Clear();
            SaveToSource();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            manipulation.ReleaseMutex();
        }
    }

    public IEnumerator<T> GetEnumerator() => coll.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => coll.GetEnumerator();
}