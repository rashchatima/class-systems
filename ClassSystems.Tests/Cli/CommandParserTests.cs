using ClassSystems.Cli.Commands;

namespace ClassSystems.Tests.Cli;

/// <summary>
/// Parsing of the commands typed by the user.
/// </summary>
public class CommandParserTests
{
    private readonly CommandParser _parser = new(new Dictionary<string, string> { ["w"] = "whole" });

    [Fact]
    public void ParseLine_SplitsSelectorArgumentsAndOptions()
    {
        // Act
        ParsedCommand? command = _parser.ParseLine("Attack conan merlin --times 2 --loud");

        // Assert
        Assert.NotNull(command);
        Assert.Equal("attack", command.Selector);
        Assert.Equal(new[] { "conan", "merlin" }, command.Arguments);
        Assert.Equal("2", command.Option("times"));
        Assert.True(command.HasOption("loud"));
        Assert.Equal(string.Empty, command.Option("loud"));
        Assert.Null(command.Option("missing"));
    }

    [Fact]
    public void ParseLine_KeepsQuotedTextTogether()
    {
        ParsedCommand? command = _parser.ParseLine("add paragraph \"Hello, world\" 'single quotes'");

        Assert.Equal(new[] { "paragraph", "Hello, world", "single quotes" }, command!.Arguments);
    }

    [Fact]
    public void ParseLine_ExpandsShortOptions()
    {
        ParsedCommand? command = _parser.ParseLine("print -w");

        Assert.True(command!.HasOption("whole"));
    }

    [Fact]
    public void ParseLine_NegativeNumberIsAnArgument()
    {
        ParsedCommand? command = _parser.ParseLine("move 3 -5");

        Assert.Equal(new[] { "3", "-5" }, command!.Arguments);
        Assert.Empty(command.Options);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseLine_EmptyLine_ReturnsNull(string line)
    {
        Assert.Null(_parser.ParseLine(line));
    }

    [Fact]
    public void ParseLine_UnclosedQuote_Throws()
    {
        Assert.Throws<FormatException>(() => _parser.ParseLine("add \"oops"));
    }

    [Fact]
    public void Parse_ProgramArgumentsWithoutSelector()
    {
        ParsedCommand command = _parser.Parse(new[] { "--data", "saves.json", "--load", "slot" });

        Assert.Equal(string.Empty, command.Selector);
        Assert.Equal("saves.json", command.Option("data"));
        Assert.Equal("slot", command.Option("load"));
    }

    [Fact]
    public void Argument_OutOfRange_ReturnsNull()
    {
        ParsedCommand command = _parser.Parse(new[] { "status" });

        Assert.Null(command.Argument(0));
    }
}
