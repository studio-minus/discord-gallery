using gallery.shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace gallery.tests.unit;

[TestClass]
public class PersistentCollectionTests
{

    [TestMethod]
    public void Persistence()
    {
        var path = Path.GetTempFileName();
        var reference = new int[]{
            1412,
            92,
            -199929,
            5023,
            "hi".GetHashCode()
        };

        {
            using PersistentCollection<int> f = new(path);
            Assert.IsTrue(File.Exists(path), "No file created");
            Assert.IsTrue(File.ReadAllBytes(path).Length == 0, "Collection is not empty");
            f.AddRange(reference);
            Assert.IsTrue(File.Exists(path), "No file created");
            Assert.IsTrue(File.ReadAllBytes(path).Length != 0, "Collection is empty after reinitialisation");
        }
        // collection is out of scope and no longer in memory

        {
            // create the collection pointing to the same path, it should load the previous data
            using PersistentCollection<int> f = new(path);
            Assert.IsTrue(File.Exists(path), "No file created");
            Assert.IsTrue(File.ReadAllBytes(path).Length != 0, "Collection is empty after reinitialisation");
            Assert.IsTrue(f.SequenceEqual(reference), "Collection does not match reference");
        }
    }

    [TestMethod]
    public void Removal()
    {
        var path = Path.GetTempFileName();
        var reference = new string[]{
            "hello",
            "can i stay",
            "three"
        };

        {
            using PersistentCollection<string> f = new(path);
            f.AddRange(reference);
        }
        // collection is out of scope and no longer in memory

        {
            // create the collection pointing to the same path, it should load the previous data
            using PersistentCollection<string> f = new(path);
            Assert.IsTrue(f.SequenceEqual(reference), "Collection does not match reference");
            f.Remove("can i stay");
            Assert.IsFalse(f.SequenceEqual(reference), "Collection matches reference despite being different");
        }
        // collection is out of scope and no longer in memory

        reference = reference.Except(new[] { "can i stay" }).ToArray(); // change reference to match new data

        {
            // create the collection pointing to the same path, it should load the previous data
            using PersistentCollection<string> f = new(path);
            Assert.IsTrue(f.SequenceEqual(reference), "Collection does not match reference");
        }
    }


    [TestMethod]
    public void ComplexObjects()
    {
        var path = Path.GetTempFileName();
        var reference = new AmComplicated[]{
            new AmComplicated(){
                Binary = false,
                IntegerColl = new AmComplicated.Nested(){ Many = new []{ 5, 2 }, Enabled = false } ,
                IntegerValue = -519,
                Interesting = TypeCode.Boolean,
                NonBinary = 1527
            },
            new AmComplicated(){
                Binary = true,
                IntegerColl = new AmComplicated.Nested(){ Many = Array.Empty<int>(), Enabled = true} ,
                IntegerValue = 1523,
                Interesting = TypeCode.String,
                NonBinary = 50892
            },
        };

        {
            using PersistentCollection<AmComplicated> f = new(path);
            Assert.IsTrue(File.Exists(path), "No file created");
            Assert.IsTrue(File.ReadAllBytes(path).Length == 0, "Collection is not empty");
            f.AddRange(reference);
            Assert.IsTrue(File.Exists(path), "No file created");
            Assert.IsTrue(File.ReadAllBytes(path).Length != 0, "Collection is empty after reinitialisation");
        }
        // collection is out of scope and no longer in memory

        {
            // create the collection pointing to the same path, it should load the previous data
            using PersistentCollection<AmComplicated> f = new(path);
            Assert.IsTrue(File.Exists(path), "No file created");
            Assert.IsTrue(File.ReadAllBytes(path).Length != 0, "Collection is empty after reinitialisation");
            Assert.IsTrue(f.SequenceEqual(reference), "Collection does not match reference");
        }
    }

    [TestMethod]
    public void ThreadSafety()
    {
        const int count = 100;
        var path = Path.GetTempFileName();
        var reference = new int[count];
        for (int i = 0; i < reference.Length; i++)
            reference[i] = i;

        int[] state;

        {
            using PersistentCollection<int> f = new(path);
            Assert.IsTrue(File.Exists(path), "No file created");
            Assert.IsTrue(File.ReadAllBytes(path).Length == 0, "Collection is not empty");
            f.AddRange(reference);

            int taskCount = 16;
            var tasks = new Task[taskCount];
            for (int i = 0; i < taskCount; i += 2)
            {
                tasks[i] = Task.Run(() =>
                {
                    var v = Random.Shared.Next(0, count);
                    f.Add(v);
                    Assert.IsTrue(f.Remove(v));
                });
                tasks[i + 1] = Task.Run(() =>
                {
                    f.Remove(Random.Shared.Next(0, count));
                });
            }

            Task.WaitAll(tasks);
            Assert.IsTrue(tasks.All(d => d.IsCompletedSuccessfully));
            state = f.ToArray();
            Assert.IsTrue(!state.SequenceEqual(reference));
        }

        {
            using PersistentCollection<int> f = new(path);
            Assert.IsTrue(File.Exists(path), "No file created");
            Assert.IsTrue(File.ReadAllBytes(path).Length != 0, "Collection is empty");

            Assert.AreEqual(state.Length, f.Count);
            Assert.IsTrue(state.SequenceEqual(f));
        }
    }

    public class AmComplicated : IEquatable<AmComplicated?>
    {
        public int IntegerValue;
        public bool Binary;
        public ushort NonBinary;

        public Nested IntegerColl;

        public TypeCode Interesting = TypeCode.SByte;

        public override bool Equals(object? obj)
        {
            return Equals(obj as AmComplicated);
        }

        public bool Equals(AmComplicated? other)
        {
            return other is not null &&
                   IntegerValue == other.IntegerValue &&
                   Binary == other.Binary &&
                   NonBinary == other.NonBinary &&
                   IntegerColl.Equals(other.IntegerColl) &&
                   Interesting == other.Interesting;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IntegerValue, Binary, NonBinary, IntegerColl, Interesting);
        }

        public static bool operator ==(AmComplicated? left, AmComplicated? right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AmComplicated? left, AmComplicated? right)
        {
            return !(left == right);
        }

        public struct Nested : IEquatable<Nested>
        {
            public int[] Many;
            public bool Enabled;

            public override bool Equals(object? obj)
            {
                return obj is Nested nested && Equals(nested);
            }

            public bool Equals(Nested other)
            {
                return Many.SequenceEqual(other.Many) &&
                       Enabled == other.Enabled;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Many, Enabled);
            }

            public static bool operator ==(Nested left, Nested right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Nested left, Nested right)
            {
                return !(left == right);
            }
        }
    }
}
