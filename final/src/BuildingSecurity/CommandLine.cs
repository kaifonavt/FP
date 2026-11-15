using System.Globalization;
using System.Text;
namespace BuildingSecurity;
public static class CommandLine
{
    public static Result<string[]> Tokenize(string line)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        char? quote = null;
        var started = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quote is not null)
            {
                if (c == quote)
                {
                    if (i + 1 < line.Length && line[i + 1] == quote) { current.Append(c); i++; }
                    else quote = null;
                }
                else current.Append(c);
            }
            else if (c is '\"' or '\'') { quote = c; started = true; }
            else if (char.IsWhiteSpace(c))
            {
                if (started) { tokens.Add(current.ToString()); current.Clear(); started = false; }
            }
            else { current.Append(c); started = true; }
        }
        if (quote is not null) return Result<string[]>.Failure(ErrorCode.InvalidInput, "Unclosed quotation mark");
        if (started) tokens.Add(current.ToString());
        return Result<string[]>.Success(tokens.ToArray());
    }

    public static Result<Dictionary<string, string>> Options(string[] tokens, params string[] allowed)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < tokens.Length; i++)
        {
            var key = tokens[i];
            if (!allowed.Contains(key) || options.ContainsKey(key))
                return Result<Dictionary<string, string>>.Failure(ErrorCode.InvalidInput, $"Unknown or duplicate option: {key}");
            if (key == "--denied-only") options.Add(key, "true");
            else
            {
                if (++i >= tokens.Length || string.IsNullOrWhiteSpace(tokens[i]) || tokens[i].StartsWith("--", StringComparison.Ordinal))
                    return Result<Dictionary<string, string>>.Failure(ErrorCode.InvalidInput, $"Missing value for {key}");
                options.Add(key, tokens[i]);
            }
        }
        return Result<Dictionary<string, string>>.Success(options);
    }

    public static bool Timestamp(string text, out DateTimeOffset value)
    {
        string[] formats = ["yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mmzzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"];
        var normalized = text.EndsWith('Z') ? text[..^1] + "+00:00" : text;
        return DateTimeOffset.TryParseExact(normalized, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
    public static bool Hours(string text, out TimeSpan value) =>
        TimeSpan.TryParseExact(text, ["hh\\:mm", "hh\\:mm\\:ss"], CultureInfo.InvariantCulture, out value) && value < TimeSpan.FromDays(1);
}
