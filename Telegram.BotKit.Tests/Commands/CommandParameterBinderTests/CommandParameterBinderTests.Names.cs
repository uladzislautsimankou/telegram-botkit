using FluentAssertions;

namespace Telegram.BotKit.Tests.Commands.CommandParameterBinderTests;

public sealed partial class CommandParameterBinderTests
{
    // verifies that the binder maps command parameters when all arguments are provided positionally
    [Fact]
    public void Bind_WhenAllPositionalArgumentsAreProvided_ShouldMapPositionalProperties()
    {
        // arrange
        var ctx = CreateContext("/cmd SomeName 25 First 2025-01-20 active=true");

        // act
        var result = _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        result.Name.Should().Be("SomeName");
        result.Age.Should().Be(25);
        result.Status.Should().Be(TestEnum.First);
        result.Date.Should().Be(new DateTime(2025, 01, 20));
        result.IsActive.Should().BeTrue();
    }

    // verifies that the binder maps command parameters when arguments are provided out of order using names
    [Fact]
    public void Bind_WhenAllNamedArgumentsAreProvided_ShouldMapNamedProperties()
    {
        // arrange
        var guid = Guid.NewGuid();
        var ctx = CreateContext($"/cmd active=true Name=SomeName Date=2025-12-31 uid={guid} Age=30 Status=Second");

        // act
        var result = _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        result.Name.Should().Be("SomeName");
        result.Age.Should().Be(30);
        result.Status.Should().Be(TestEnum.Second);
        result.Date.Should().Be(new DateTime(2025, 12, 31));
        result.UserId.Should().Be(guid);
        result.IsActive.Should().BeTrue();
    }

    // verifies that a mixture of positional and named parameters is matched correctly by skipping named arguments in the positional sequence
    [Fact]
    public void Bind_WhenHybridArgumentsAreProvided_ShouldMapHybridProperties()
    {
        // arrange
        var ctx = CreateContext("/cmd SomeName Age=99 Second 2022-02-02 active=true");

        // act
        var result = _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        result.Name.Should().Be("SomeName");
        result.Age.Should().Be(99);
        result.Status.Should().Be(TestEnum.Second);
        result.Date.Should().Be(new DateTime(2022, 02, 02));
    }

    // verifies that a positional value containing '=' stays positional when the part before '=' is not a known parameter name
    [Theory]
    [InlineData("/search https://x.com/?id=5", "https://x.com/?id=5")]
    [InlineData("/search a=b", "a=b")]
    [InlineData("/search a=b c=d", "a=b c=d")]
    [InlineData("/search =5", "=5")]
    public void Bind_WhenPositionalValueContainsEqualsSign_ShouldKeepItAsPositional(string text, string expected)
    {
        // arrange
        var ctx = CreateContext(text);

        // act
        var result = _binder.Bind<SearchCommandParams>(ctx);

        // assert
        result.Query.Should().Be(expected);
    }

    // verifies that named argument names are matched case-insensitively and do not consume positional slots
    [Fact]
    public void Bind_WhenNamedArgumentUsesDifferentCase_ShouldMapItAndKeepPositionalOrder()
    {
        // arrange
        var ctx = CreateContext("/cmd NAME=Bob 1 First 2025-01-01 active=true");

        // act
        var result = _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        result.Name.Should().Be("Bob");
        result.Age.Should().Be(1);
        result.Status.Should().Be(TestEnum.First);
    }
}
