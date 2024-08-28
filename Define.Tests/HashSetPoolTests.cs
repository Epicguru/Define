using Xunit.Abstractions;

namespace Define.Tests;

public class HashSetPoolTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void RentReturnsValidSet()
    {
        using var _ = HashSetPool<int>.Rent(out var set);

        set.Should().NotBeNull();
        set.Count.Should().Be(0);
    }

    [Fact]
    public void ReturnedSetIsPooledAndClearWorks()
    {
        HashSet<int> set;
        HashSetPool<int>.PooledCount.Should().Be(0);

        using (var _ = HashSetPool<int>.Rent(out set))
        {
            HashSetPool<int>.PooledCount.Should().Be(0);
            set.Should().NotBeNull();
            set.Count.Should().Be(0);
            set.Add(123);
        }

        HashSetPool<int>.PooledCount.Should().Be(1);
        set.Should().BeEmpty();

        HashSetPool<int>.Clear();
        HashSetPool<int>.PooledCount.Should().Be(0);
    }

    [Fact]
    public void MultipleRentWorks()
    {
        using var _ = HashSetPool<int>.Rent(out var set);
        using var _2 = HashSetPool<int>.Rent(out var set2);

        set.Should().NotBeNull();
        set2.Should().NotBeNull();
        set.Count.Should().Be(0);
        set2.Count.Should().Be(0);
        set.Should().NotBeSameAs(set2);
    }

    public override void Dispose()
    {
        base.Dispose();
        HashSetPool<int>.Clear();
    }
}