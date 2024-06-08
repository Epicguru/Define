using System.Collections;

namespace TestSharedLib;

public struct ArgMatrix : IEnumerable<object[]>
{
    private List<object[]> args;

    public void Add(Span<object> arg)
    {
        args ??= [];
        args.Add(arg.ToArray());
    }

    public readonly IEnumerable<object[]> MakeArgs()
    {
        if (args == null)
            yield break;

        int totalPermutations = args.Aggregate(1, (a, b) => a * b.Length);
        int[] sums = new int[args.Count];
        int prev = 0;
        for (int i = args.Count - 1; i >= 0; i--)
        {
            sums[i] = prev;
            prev += args[i].Length;
        }

        for (int i = 0; i < totalPermutations; i++)
        {
            var output = new object[args.Count];

            for (int j = 0; j < args.Count; j++)
            {
                int index = (sums[j] == 0 ? i : i / sums[j]) % args[j].Length;
                output[j] = args[j][index];
            }

            yield return output;
        }
    }

    public readonly IEnumerator<object[]> GetEnumerator() => args.GetEnumerator();

    readonly IEnumerator IEnumerable.GetEnumerator() => args.GetEnumerator();
}