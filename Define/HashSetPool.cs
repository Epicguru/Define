using System.Diagnostics;

namespace Define;

internal static class HashSetPool<T>
{
    public const int DEFAULT_CAPACITY = 32;

    public static int PooledCount => pool.Count;

    private static readonly Queue<HashSet<T>> pool = new Queue<HashSet<T>>();

    public static void Clear()
    {
        pool.Clear();
    }

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