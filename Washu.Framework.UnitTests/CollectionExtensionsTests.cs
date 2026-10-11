namespace Washu.Framework.UnitTests;

using FluentAssertions;
using Washu.Framework.Extensions;

public class CollectionExtensionsTests
{
    [Fact]
    public void IsEmpty_ReturnsTrue_WhenCollectionHasNoElements()
    {
        var emptyList = new List<int>();
        emptyList.IsEmpty.Should().BeTrue("an empty list should be empty");
        emptyList.IsNotEmpty.Should().BeFalse("an empty list should not be not-empty");
    }

    [Fact]
    public void IsNotEmpty_ReturnsTrue_WhenCollectionHasElements()
    {
        var populatedList = new List<string> { "a", "b" };
        populatedList.IsNotEmpty.Should().BeTrue("a populated list should be not-empty");
        populatedList.IsEmpty.Should().BeFalse("a populated list should not be empty");
    }
}
