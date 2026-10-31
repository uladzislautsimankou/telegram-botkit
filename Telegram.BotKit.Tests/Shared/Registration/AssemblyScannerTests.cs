using FluentAssertions;
using Telegram.BotKit.Shared.Registration;

namespace Telegram.BotKit.Tests.Shared.Registration;

public sealed class AssemblyScannerTests
{
    public interface IScanMarker;

    public sealed class ZetaHandler : IScanMarker;
    public sealed class AlphaHandler : IScanMarker;
    public abstract class AbstractHandler : IScanMarker;
    public sealed class GenericHandler<T> : IScanMarker;
    public sealed class UnrelatedType;

    // verifies that only concrete non-generic matching types are returned, ordered by full name
    [Fact]
    public void FindTypes_ShouldReturnConcreteMatchingTypesOrderedByFullName()
    {
        var types = AssemblyScanner.FindTypes(
            [typeof(AssemblyScannerTests).Assembly],
            t => typeof(IScanMarker).IsAssignableFrom(t));

        types.Should().Equal(typeof(AlphaHandler), typeof(ZetaHandler));
    }

    // verifies that the same assembly passed twice does not duplicate types
    [Fact]
    public void FindTypes_WhenAssemblyIsPassedTwice_ShouldNotDuplicateTypes()
    {
        var assembly = typeof(AssemblyScannerTests).Assembly;

        var types = AssemblyScanner.FindTypes([assembly, assembly], t => typeof(IScanMarker).IsAssignableFrom(t));

        types.Should().HaveCount(2);
    }

    // verifies detection of closed generic interface implementations
    [Fact]
    public void Implements_WhenTypeImplementsClosedGenericInterface_ShouldReturnTrue()
    {
        AssemblyScanner.Implements(typeof(List<int>), typeof(IEnumerable<>)).Should().BeTrue();
        AssemblyScanner.Implements(typeof(UnrelatedType), typeof(IEnumerable<>)).Should().BeFalse();
    }
}
