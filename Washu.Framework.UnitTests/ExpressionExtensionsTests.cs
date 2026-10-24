namespace Washu.Framework.UnitTests;

using System.Linq.Expressions;
using FluentAssertions;
using Washu.Framework.Extensions;

public class ExpressionExtensionsTests
{
    private sealed class SampleModel
    {
        public string Name { get; } = string.Empty;
        public int Age { get; set; }
        public string GetName() => Name;
    }

    [Fact]
    public void PropertyInfo_DirectProperty_ReturnsCorrectPropertyInfo()
    {
        Expression<Func<SampleModel, string>> expr = x => x.Name;

        var propInfo = expr.PropertyInfo;

        propInfo.Should().NotBeNull();
        propInfo.Name.Should().Be(nameof(SampleModel.Name));
        propInfo.PropertyType.Should().Be<string>();
        propInfo.DeclaringType.Should().Be<SampleModel>();
    }

    [Fact]
    public void PropertyInfo_ValueTypePropertyWithConvertUnaryExpression_UnwrapsAndReturnsPropertyInfo()
    {
        // When boxing int to object, compiler creates a UnaryExpression with ExpressionType.Convert
        Expression<Func<SampleModel, object>> expr = x => x.Age;

        var propInfo = expr.PropertyInfo;

        propInfo.Should().NotBeNull();
        propInfo.Name.Should().Be(nameof(SampleModel.Age));
        propInfo.PropertyType.Should().Be<int>();
    }

    [Fact]
    public void PropertyInfo_MethodCallExpression_ThrowsArgumentException()
    {
        Expression<Func<SampleModel, string>> expr = x => x.GetName();

        var act = () => _ = expr.PropertyInfo;

        act.Should().Throw<ArgumentException>()
            .WithMessage("*The expression must resolve directly to a property*")
            .WithParameterName("property");
    }

    [Fact]
    public void PropertyInfo_ConstantExpression_ThrowsArgumentException()
    {
        Expression<Func<SampleModel, int>> expr = x => 42;

        var act = () => _ = expr.PropertyInfo;

        act.Should().Throw<ArgumentException>()
            .WithMessage("*The expression must resolve directly to a property*")
            .WithParameterName("property");
    }
}
