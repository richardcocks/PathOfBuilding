using System.Globalization;
using System.Text;

namespace PathOfBuilding.Core.Tree;

/// <summary>
/// Parses Lua table literals (as found in tree.lua files) into C# objects.
/// Handles: strings, numbers, booleans, nested tables with both array and dict entries.
/// Returns Dictionary&lt;string, object?&gt; for dict tables, List&lt;object?&gt; for array tables,
/// and string/double/bool for primitives.
/// </summary>
public static class LuaTableParser
{
    /// <summary>
    /// Parse a Lua file that returns a table literal (e.g., "return { ... }").
    /// </summary>
    public static object? ParseFile(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return Parse(text);
    }

    /// <summary>
    /// Parse a Lua table literal string.
    /// Handles "return { ... }" wrapper or bare "{ ... }".
    /// </summary>
    public static object? Parse(string lua)
    {
        var reader = new LuaReader(lua);
        reader.SkipWhitespaceAndComments();

        // Skip optional "return" keyword
        if (reader.TryConsume("return"))
            reader.SkipWhitespaceAndComments();

        return ParseValue(ref reader);
    }

    private static object? ParseValue(ref LuaReader reader)
    {
        reader.SkipWhitespaceAndComments();

        if (reader.IsAtEnd)
            return null;

        char c = reader.Peek();

        if (c == '{')
            return ParseTable(ref reader);
        if (c == '"')
            return ParseString(ref reader);
        if (c == '-' || char.IsDigit(c))
            return ParseNumber(ref reader);
        if (reader.TryConsume("true"))
            return true;
        if (reader.TryConsume("false"))
            return false;
        if (reader.TryConsume("nil"))
            return null;

        throw new FormatException($"Unexpected character '{c}' at position {reader.Position}");
    }

    private static object ParseTable(ref LuaReader reader)
    {
        reader.Expect('{');
        reader.SkipWhitespaceAndComments();

        var dict = new Dictionary<string, object?>();
        var list = new List<object?>();
        bool hasDictEntries = false;
        bool hasArrayEntries = false;

        while (!reader.IsAtEnd && reader.Peek() != '}')
        {
            reader.SkipWhitespaceAndComments();
            if (reader.Peek() == '}')
                break;

            if (reader.Peek() == '[')
            {
                // Dict entry: [key]= value
                reader.Expect('[');
                reader.SkipWhitespaceAndComments();

                string key;
                if (reader.Peek() == '"')
                {
                    key = ParseString(ref reader);
                }
                else
                {
                    // Numeric key
                    key = ParseNumericKey(ref reader);
                }

                reader.SkipWhitespaceAndComments();
                reader.Expect(']');
                reader.SkipWhitespaceAndComments();
                reader.Expect('=');
                reader.SkipWhitespaceAndComments();

                var value = ParseValue(ref reader);
                dict[key] = value;
                hasDictEntries = true;
            }
            else
            {
                // Array entry (no key)
                var value = ParseValue(ref reader);
                list.Add(value);
                hasArrayEntries = true;
            }

            reader.SkipWhitespaceAndComments();
            // Optional comma
            if (!reader.IsAtEnd && reader.Peek() == ',')
                reader.Advance();
        }

        reader.Expect('}');

        // Return dict if has dict entries, array otherwise
        // Mixed tables: merge array entries into dict with numeric string keys
        if (hasDictEntries && hasArrayEntries)
        {
            for (int i = 0; i < list.Count; i++)
                dict[(i + 1).ToString()] = list[i];
            return dict;
        }
        if (hasDictEntries)
            return dict;
        return list;
    }

    private static string ParseString(ref LuaReader reader)
    {
        reader.Expect('"');
        var sb = new StringBuilder();

        while (!reader.IsAtEnd && reader.Peek() != '"')
        {
            char c = reader.Read();
            if (c == '\\')
            {
                if (reader.IsAtEnd)
                    throw new FormatException("Unterminated string escape");
                char escaped = reader.Read();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => escaped,
                });
            }
            else
            {
                sb.Append(c);
            }
        }

        reader.Expect('"');
        return sb.ToString();
    }

    private static double ParseNumber(ref LuaReader reader)
    {
        int start = reader.Position;
        if (reader.Peek() == '-')
            reader.Advance();

        while (!reader.IsAtEnd && (char.IsDigit(reader.Peek()) || reader.Peek() == '.'))
            reader.Advance();

        // Handle scientific notation (e.g., 1.5e10)
        if (!reader.IsAtEnd && (reader.Peek() == 'e' || reader.Peek() == 'E'))
        {
            reader.Advance();
            if (!reader.IsAtEnd && (reader.Peek() == '+' || reader.Peek() == '-'))
                reader.Advance();
            while (!reader.IsAtEnd && char.IsDigit(reader.Peek()))
                reader.Advance();
        }

        var numStr = reader.Substring(start, reader.Position - start);
        return double.Parse(numStr, CultureInfo.InvariantCulture);
    }

    private static string ParseNumericKey(ref LuaReader reader)
    {
        int start = reader.Position;
        if (reader.Peek() == '-')
            reader.Advance();

        while (!reader.IsAtEnd && (char.IsDigit(reader.Peek()) || reader.Peek() == '.'))
            reader.Advance();

        return reader.Substring(start, reader.Position - start);
    }

    /// <summary>
    /// Lightweight string reader with comment skipping.
    /// </summary>
    private ref struct LuaReader
    {
        private readonly ReadOnlySpan<char> _text;
        private int _pos;

        public LuaReader(string text)
        {
            _text = text.AsSpan();
            _pos = 0;
        }

        public int Position => _pos;
        public bool IsAtEnd => _pos >= _text.Length;

        public char Peek() => _text[_pos];

        public char Read() => _text[_pos++];

        public void Advance() => _pos++;

        public string Substring(int start, int length)
            => _text.Slice(start, length).ToString();

        public void Expect(char c)
        {
            if (IsAtEnd || _text[_pos] != c)
                throw new FormatException($"Expected '{c}' at position {_pos}, got '{(IsAtEnd ? "EOF" : _text[_pos].ToString())}'");
            _pos++;
        }

        public bool TryConsume(string keyword)
        {
            if (_pos + keyword.Length > _text.Length)
                return false;

            for (int i = 0; i < keyword.Length; i++)
            {
                if (_text[_pos + i] != keyword[i])
                    return false;
            }

            // Ensure it's not part of a longer identifier
            if (_pos + keyword.Length < _text.Length && char.IsLetterOrDigit(_text[_pos + keyword.Length]))
                return false;

            _pos += keyword.Length;
            return true;
        }

        public void SkipWhitespaceAndComments()
        {
            while (!IsAtEnd)
            {
                char c = _text[_pos];
                if (char.IsWhiteSpace(c))
                {
                    _pos++;
                    continue;
                }

                // Lua line comment: --
                if (c == '-' && _pos + 1 < _text.Length && _text[_pos + 1] == '-')
                {
                    // Check for block comment: --[[ ... ]]
                    if (_pos + 3 < _text.Length && _text[_pos + 2] == '[' && _text[_pos + 3] == '[')
                    {
                        _pos += 4;
                        while (!IsAtEnd)
                        {
                            if (_text[_pos] == ']' && _pos + 1 < _text.Length && _text[_pos + 1] == ']')
                            {
                                _pos += 2;
                                break;
                            }
                            _pos++;
                        }
                    }
                    else
                    {
                        // Skip to end of line
                        while (!IsAtEnd && _text[_pos] != '\n')
                            _pos++;
                    }
                    continue;
                }

                break;
            }
        }
    }
}
