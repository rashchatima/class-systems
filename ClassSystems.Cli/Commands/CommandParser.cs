using System.Text;

namespace ClassSystems.Cli.Commands;

/// <summary>
/// Turns a line of text or the program arguments into a <see cref="ParsedCommand"/>.
/// </summary>
/// <remarks>
/// Supported notation: <c>call arg1 "arg with spaces" --option value -o value --flag</c>.
/// Short options (<c>-o</c>) are expanded with the given aliases, for example <c>-w</c> to <c>whole</c>.
/// Negative numbers such as <c>-5</c> are treated as arguments, not options.
/// </remarks>
public class CommandParser
{
    private readonly IReadOnlyDictionary<string, string> _shortOptions;

    /// <param name="shortOptions">Maps one-letter options to their full names.</param>
    public CommandParser(IReadOnlyDictionary<string, string>? shortOptions = null)
    {
        _shortOptions = shortOptions ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Parses a line typed by the user.
    /// </summary>
    /// <returns>The parsed command, or <c>null</c> for an empty line.</returns>
    /// <exception cref="FormatException">A quote is not closed.</exception>
    public ParsedCommand? ParseLine(string line)
    {
        List<string> tokens = Tokenize(line);
        return tokens.Count == 0 ? null : Parse(tokens);
    }

    /// <summary>
    /// Parses already separated tokens, for example the program arguments.
    /// The first token is the selector; pass an empty selector for program options only.
    /// </summary>
    public ParsedCommand Parse(IReadOnlyList<string> tokens)
    {
        var arguments = new List<string>();
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string selector = tokens.Count > 0 && !IsOption(tokens[0]) ? tokens[0] : string.Empty;
        int index = selector.Length > 0 ? 1 : 0;

        while (index < tokens.Count)
        {
            string token = tokens[index++];

            if (!IsOption(token))
            {
                arguments.Add(token);
                continue;
            }

            string name = ExpandOption(token);
            bool hasValue = index < tokens.Count && !IsOption(tokens[index]);
            options[name] = hasValue ? tokens[index++] : string.Empty;
        }

        return new ParsedCommand(selector.ToLowerInvariant(), arguments, options);
    }

    // Splits a line by spaces, keeping text in double or single quotes together.
    private static List<string> Tokenize(string line)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        char? quote = null;
        bool inToken = false;

        foreach (char symbol in line)
        {
            if (quote != null)
            {
                if (symbol == quote)
                    quote = null;
                else
                    current.Append(symbol);
            }
            else if (symbol is '"' or '\'')
            {
                quote = symbol;
                inToken = true;
            }
            else if (char.IsWhiteSpace(symbol))
            {
                if (inToken)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    inToken = false;
                }
            }
            else
            {
                current.Append(symbol);
                inToken = true;
            }
        }

        if (quote != null)
            throw new FormatException("A quote is not closed.");

        if (inToken)
            tokens.Add(current.ToString());

        return tokens;
    }

    private static bool IsOption(string token)
        => token.StartsWith("--") && token.Length > 2
           || token.StartsWith('-') && token.Length == 2 && char.IsLetter(token[1]);

    private string ExpandOption(string token)
    {
        if (token.StartsWith("--"))
            return token[2..].ToLowerInvariant();

        string letter = token[1..].ToLowerInvariant();
        return _shortOptions.TryGetValue(letter, out string? fullName) ? fullName : letter;
    }
}
