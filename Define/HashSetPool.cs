using System.Collections.Concurrent;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Define;

internal static class HashSetPool<T>
{
    private const int DEFAULT_CAPACITY = 32;

    public static int PooledCount => pool.Count;

    private static readonly ConcurrentQueue<HashSet<T>> pool = new ConcurrentQueue<HashSet<T>>();

    public static void Clear()
    {
        pool.Clear();
    }

    [MustDisposeResource]
    public static Using Rent(out HashSet<T> set)
    {
        set = RentOrCreate();
        return new Using(set);
    }

    private static HashSet<T> RentOrCreate()
    {
        if (pool.TryDequeue(out var dq))
        {
            return dq;
        }

        dq = new HashSet<T>(DEFAULT_CAPACITY);
        return dq;
    }

    private static void Return(HashSet<T> set)
    {
        Debug.Assert(set != null);
        Debug.Assert(!pool.Contains(set));

        set.Clear();
        pool.Enqueue(set);
    }

    internal readonly ref struct Using
    {
        private readonly HashSet<T> set;

        public Using(HashSet<T> set)
        {
            this.set = set;
        }

        public void Dispose()
        {
            Return(set);
        }
    }
}