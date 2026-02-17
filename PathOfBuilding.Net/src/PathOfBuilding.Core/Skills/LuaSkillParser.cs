using System.Globalization;
using System.Text;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Represents a dotted identifier like SkillType.Spell or ModFlag.Attack.
/// </summary>
internal sealed record DottedIdentifier(string Namespace, string Name);

/// <summary>
/// Represents a function call like mod("Damage", "MORE", nil) or bit.bor(a, b).
/// </summary>
internal sealed record FunctionCall(string Name, List<object?> Args);

/// <summary>
/// Parses Lua skill DSL files into C# objects.
/// Extends LuaTableParser with support for:
/// - Bare identifier keys (name = "Arc")
/// - Single-quoted strings ('text')
/// - Dotted identifier values (SkillType.Spell)
/// - Function calls (mod("Damage", "MORE", nil))
/// - Function literal skipping (function(...) ... end)
/// - Assignment parsing (skills["Arc"] = { ... })
/// - Negative numbers prefixed with minus
/// - String concatenation with ..
/// </summary>
internal static class LuaSkillParser
{
    /// <summary>
    /// Parse a skill file with "local skills, mod, flag, skill = ..." preamble
    /// and "skills["X"] = { ... }" assignments.
    /// Returns dict mapping skill id → parsed table.
    /// </summary>
    public static Dictionary<string, object?> ParseSkillFile(string lua)
    {
        var reader = new LuaReader(lua);
        var result = new Dictionary<string, object?>();

        reader.SkipWhitespaceAndComments();

        // Skip preamble: "local skills, mod, flag, skill = ..."
        SkipPreamble(ref reader);

        // Parse assignments: skills["X"] = { ... }
        while (!reader.IsAtEnd)
        {
            reader.SkipWhitespaceAndComments();
            if (reader.IsAtEnd) break;

            // Expect: skills["key"] = value
            if (!reader.TryConsume("skills"))
                break;

            reader.SkipWhitespaceAndComments();
            reader.Expect('[');
            reader.SkipWhitespaceAndComments();
            var key = ParseString(ref reader);
            reader.SkipWhitespaceAndComments();
            reader.Expect(']');
            reader.SkipWhitespaceAndComments();
            reader.Expect('=');
            reader.SkipWhitespaceAndComments();

            var value = ParseValue(ref reader);
            result[key] = value;

            reader.SkipWhitespaceAndComments();
        }

        return result;
    }

    /// <summary>
    /// Parse a file that returns a table literal (Gems.lua, SkillStatMap.lua).
    /// Handles optional "local mod, flag, skill = ..." preamble.
    /// </summary>
    public static object? ParseReturnTable(string lua)
    {
        var reader = new LuaReader(lua);
        reader.SkipWhitespaceAndComments();

        // Skip optional local preamble
        SkipPreamble(ref reader);

        reader.SkipWhitespaceAndComments();

        // Skip "return" keyword
        if (reader.TryConsume("return"))
            reader.SkipWhitespaceAndComments();

        return ParseValue(ref reader);
    }

    private static void SkipPreamble(ref LuaReader reader)
    {
        // Skip "local ..." lines (variable declarations)
        while (!reader.IsAtEnd && reader.TryConsume("local"))
        {
            // Skip to end of statement (newline or semicolon)
            while (!reader.IsAtEnd)
            {
                char c = reader.Peek();
                if (c == '\n' || c == '\r')
                {
                    reader.Advance();
                    break;
                }
                reader.Advance();
            }
            reader.SkipWhitespaceAndComments();
        }
    }

    internal static object? ParseValue(ref LuaReader reader)
    {
        reader.SkipWhitespaceAndComments();

        if (reader.IsAtEnd)
            return null;

        char c = reader.Peek();

        object? result;

        if (c == '{')
            return ParseTable(ref reader);
        else if (c == '"')
            return ParseString(ref reader);
        else if (c == '\'')
            return ParseSingleQuotedString(ref reader);
        else if (c == '-' && reader.HasNext() && (char.IsDigit(reader.PeekAt(1)) || reader.PeekAt(1) == '.'))
            result = ParseNumber(ref reader);
        else if (char.IsDigit(c))
            result = ParseNumber(ref reader);
        else if (reader.TryConsume("true"))
            return true;
        else if (reader.TryConsume("false"))
            return false;
        else if (reader.TryConsume("nil"))
            return null;
        else if (reader.TryConsume("function"))
            return SkipFunctionLiteral(ref reader);
        else if (char.IsLetter(c) || c == '_')
            return ParseIdentifierOrCall(ref reader);
        else
            throw new FormatException($"Unexpected character '{c}' at position {reader.Position}");

        // Handle arithmetic operators after numeric values: +, -, *, /
        return TryParseArithmetic(ref reader, (double)result);
    }

    private static double TryParseArithmetic(ref LuaReader reader, double left)
    {
        while (true)
        {
            reader.SkipWhitespaceAndComments();
            if (reader.IsAtEnd) return left;

            char op = reader.Peek();
            if (op != '+' && op != '*' && op != '/' && op != '%')
            {
                // Handle '-' only if followed by digit/space (not part of negative number in table)
                if (op == '-')
                {
                    // Check it's a binary minus, not comment (--)
                    if (reader.HasNext() && reader.PeekAt(1) == '-')
                        return left;
                }
                else
                {
                    return left;
                }
            }

            // It's an operator
            if (op != '+' && op != '-' && op != '*' && op != '/' && op != '%')
                return left;

            reader.Advance(); // consume operator
            reader.SkipWhitespaceAndComments();

            double right;
            if (!reader.IsAtEnd && (char.IsDigit(reader.Peek()) || reader.Peek() == '-' || reader.Peek() == '.'))
            {
                right = ParseNumber(ref reader);
            }
            else
            {
                throw new FormatException($"Expected number after operator '{op}' at position {reader.Position}");
            }

            left = op switch
            {
                '+' => left + right,
                '-' => left - right,
                '*' => left * right,
                '/' => left / right,
                '%' => left % right,
                _ => left,
            };
        }
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
            if (reader.IsAtEnd || reader.Peek() == '}')
                break;

            // Check for dict-style entries
            if (reader.Peek() == '[')
            {
                // [key] = value
                reader.Expect('[');
                reader.SkipWhitespaceAndComments();

                string key;
                if (reader.Peek() == '"' || reader.Peek() == '\'')
                {
                    key = reader.Peek() == '"' ? ParseString(ref reader) : ParseSingleQuotedString(ref reader);
                }
                else
                {
                    // Numeric or identifier key
                    key = ParseTableKey(ref reader);
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
            else if (IsBareDictEntry(ref reader))
            {
                // bare_key = value
                var key = ParseIdentifierName(ref reader);
                reader.SkipWhitespaceAndComments();
                reader.Expect('=');
                reader.SkipWhitespaceAndComments();
                var value = ParseValue(ref reader);
                dict[key] = value;
                hasDictEntries = true;
            }
            else
            {
                // Array entry
                var value = ParseValue(ref reader);
                list.Add(value);
                hasArrayEntries = true;
            }

            reader.SkipWhitespaceAndComments();
            // Optional comma or semicolon
            if (!reader.IsAtEnd && (reader.Peek() == ',' || reader.Peek() == ';'))
                reader.Advance();
        }

        reader.Expect('}');

        // Mixed tables: merge array entries into dict with 1-based string keys
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

    /// <summary>
    /// Check if current position is a bare identifier key followed by '='.
    /// Must not consume any characters — just peek ahead.
    /// </summary>
    private static bool IsBareDictEntry(ref LuaReader reader)
    {
        if (reader.IsAtEnd) return false;
        char c = reader.Peek();
        if (!char.IsLetter(c) && c != '_') return false;

        // Scan ahead to find the identifier end, then check for '='
        int pos = reader.Position;
        int i = pos;
        while (i < reader.Length)
        {
            char ch = reader.CharAt(i);
            if (!char.IsLetterOrDigit(ch) && ch != '_') break;
            i++;
        }

        // Skip whitespace
        while (i < reader.Length && char.IsWhiteSpace(reader.CharAt(i)))
            i++;

        // Check for '=' but not '=='
        return i < reader.Length && reader.CharAt(i) == '=' && (i + 1 >= reader.Length || reader.CharAt(i + 1) != '=');
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

        // Handle string concatenation with ..
        reader.SkipWhitespaceAndComments();
        if (!reader.IsAtEnd && reader.Peek() == '.' && reader.HasNext() && reader.PeekAt(1) == '.')
        {
            reader.Advance(); // skip first .
            reader.Advance(); // skip second .
            reader.SkipWhitespaceAndComments();
            var right = ParseValue(ref reader);
            sb.Append(right?.ToString() ?? "");
        }

        return sb.ToString();
    }

    private static string ParseSingleQuotedString(ref LuaReader reader)
    {
        reader.Expect('\'');
        var sb = new StringBuilder();

        while (!reader.IsAtEnd && reader.Peek() != '\'')
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

        reader.Expect('\'');

        // Handle string concatenation with ..
        reader.SkipWhitespaceAndComments();
        if (!reader.IsAtEnd && reader.Peek() == '.' && reader.HasNext() && reader.PeekAt(1) == '.')
        {
            reader.Advance();
            reader.Advance();
            reader.SkipWhitespaceAndComments();
            var right = ParseValue(ref reader);
            sb.Append(right?.ToString() ?? "");
        }

        return sb.ToString();
    }

    private static double ParseNumber(ref LuaReader reader)
    {
        int start = reader.Position;
        if (reader.Peek() == '-')
            reader.Advance();

        // Handle hex: 0x...
        if (!reader.IsAtEnd && reader.Peek() == '0' && reader.HasNext() &&
            (reader.PeekAt(1) == 'x' || reader.PeekAt(1) == 'X'))
        {
            reader.Advance(); // 0
            reader.Advance(); // x
            while (!reader.IsAtEnd && IsHexDigit(reader.Peek()))
                reader.Advance();
            var hexStr = reader.Substring(start, reader.Position - start);
            return Convert.ToInt64(hexStr.Replace("0x", "").Replace("0X", ""), 16);
        }

        while (!reader.IsAtEnd && (char.IsDigit(reader.Peek()) || reader.Peek() == '.'))
            reader.Advance();

        // Scientific notation
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

    private static bool IsHexDigit(char c) =>
        char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private static string ParseTableKey(ref LuaReader reader)
    {
        // Could be a numeric key, identifier, or dotted identifier resolved to number
        if (char.IsDigit(reader.Peek()) || reader.Peek() == '-')
        {
            int start = reader.Position;
            if (reader.Peek() == '-') reader.Advance();
            while (!reader.IsAtEnd && (char.IsDigit(reader.Peek()) || reader.Peek() == '.'))
                reader.Advance();
            return reader.Substring(start, reader.Position - start);
        }

        // Identifier key like SkillType.Spell → store as "SkillType.Spell" string
        var ident = ParseIdentifierOrCall(ref reader);
        if (ident is DottedIdentifier di)
            return string.IsNullOrEmpty(di.Namespace) ? di.Name : $"{di.Namespace}.{di.Name}";
        return ident?.ToString() ?? "nil";
    }

    private static string ParseIdentifierName(ref LuaReader reader)
    {
        int start = reader.Position;
        while (!reader.IsAtEnd && (char.IsLetterOrDigit(reader.Peek()) || reader.Peek() == '_'))
            reader.Advance();
        return reader.Substring(start, reader.Position - start);
    }

    private static object? ParseIdentifierOrCall(ref LuaReader reader)
    {
        string name = ParseIdentifierName(ref reader);

        // Consume dot-chained segments: name.sub.sub2...
        // Build a list of segments for multi-level access (e.g., skills.ExplosiveTrap.parts)
        var segments = new List<string> { name };
        while (!reader.IsAtEnd && reader.Peek() == '.')
        {
            reader.Advance(); // skip .
            string seg = ParseIdentifierName(ref reader);
            segments.Add(seg);
        }

        string fullName = string.Join(".", segments);

        // Check for function call: name.sub(...)
        reader.SkipWhitespaceAndComments();
        if (!reader.IsAtEnd && reader.Peek() == '(')
        {
            var args = ParseArgList(ref reader);
            return new FunctionCall(fullName, args);
        }

        if (segments.Count == 1)
            return new DottedIdentifier("", segments[0]);
        if (segments.Count == 2)
            return new DottedIdentifier(segments[0], segments[1]);

        // Multi-segment access (e.g., skills.ExplosiveTrap.parts) — return as DottedIdentifier
        // with the first part as namespace and rest joined as name
        return new DottedIdentifier(segments[0], string.Join(".", segments.Skip(1)));
    }

    private static List<object?> ParseArgList(ref LuaReader reader)
    {
        reader.Expect('(');
        var args = new List<object?>();

        reader.SkipWhitespaceAndComments();
        while (!reader.IsAtEnd && reader.Peek() != ')')
        {
            var arg = ParseValue(ref reader);
            args.Add(arg);

            reader.SkipWhitespaceAndComments();
            if (!reader.IsAtEnd && reader.Peek() == ',')
                reader.Advance();
            reader.SkipWhitespaceAndComments();
        }

        reader.Expect(')');
        return args;
    }

    /// <summary>
    /// Skip a function literal: function(...) ... end
    /// Must handle nested blocks (if/end, for/end, while/end, do/end, repeat/until, function/end).
    /// </summary>
    private static object? SkipFunctionLiteral(ref LuaReader reader)
    {
        // Already consumed "function" keyword
        // Skip params: (...)
        reader.SkipWhitespaceAndComments();
        if (!reader.IsAtEnd && reader.Peek() == '(')
        {
            int depth = 1;
            reader.Advance(); // skip (
            while (!reader.IsAtEnd && depth > 0)
            {
                if (reader.Peek() == '(') depth++;
                else if (reader.Peek() == ')') depth--;
                if (depth > 0) reader.Advance();
            }
            reader.Expect(')');
        }

        // Skip body until matching "end"
        int blockDepth = 1;
        while (!reader.IsAtEnd && blockDepth > 0)
        {
            reader.SkipWhitespaceAndComments();
            if (reader.IsAtEnd) break;

            char c = reader.Peek();

            // Skip strings to avoid false keyword matches
            if (c == '"')
            {
                SkipString(ref reader, '"');
                continue;
            }
            if (c == '\'')
            {
                SkipString(ref reader, '\'');
                continue;
            }
            // Skip long strings [[...]]
            if (c == '[' && reader.HasNext() && reader.PeekAt(1) == '[')
            {
                reader.Advance();
                reader.Advance();
                while (!reader.IsAtEnd)
                {
                    if (reader.Peek() == ']' && reader.HasNext() && reader.PeekAt(1) == ']')
                    {
                        reader.Advance();
                        reader.Advance();
                        break;
                    }
                    reader.Advance();
                }
                continue;
            }

            // Check for block-opening keywords
            if (char.IsLetter(c))
            {
                if (reader.TryConsumeKeyword("function")) { blockDepth++; continue; }
                if (reader.TryConsumeKeyword("if")) { blockDepth++; continue; }
                // for..do..end and while..do..end each count as one block
                // The 'do' is consumed below but not counted as an extra block
                if (reader.TryConsumeKeyword("for")) { blockDepth++; continue; }
                if (reader.TryConsumeKeyword("while")) { blockDepth++; continue; }
                if (reader.TryConsumeKeyword("do")) { /* part of for/while, not a separate block */ continue; }
                if (reader.TryConsumeKeyword("repeat")) { blockDepth++; continue; }
                if (reader.TryConsumeKeyword("end"))
                {
                    blockDepth--;
                    continue;
                }
                if (reader.TryConsumeKeyword("until"))
                {
                    blockDepth--; // matches repeat
                    // Skip condition expression to end of line
                    while (!reader.IsAtEnd && reader.Peek() != '\n')
                        reader.Advance();
                    continue;
                }
                // Skip other identifiers
                while (!reader.IsAtEnd && (char.IsLetterOrDigit(reader.Peek()) || reader.Peek() == '_'))
                    reader.Advance();
                continue;
            }

            reader.Advance();
        }

        return null; // Function literals are not interpreted
    }

    private static void SkipString(ref LuaReader reader, char quote)
    {
        reader.Advance(); // skip opening quote
        while (!reader.IsAtEnd && reader.Peek() != quote)
        {
            if (reader.Peek() == '\\')
            {
                reader.Advance(); // skip backslash
                if (!reader.IsAtEnd) reader.Advance(); // skip escaped char
            }
            else
            {
                reader.Advance();
            }
        }
        if (!reader.IsAtEnd) reader.Advance(); // skip closing quote
    }

    /// <summary>
    /// Lightweight reader for Lua source text with comment and whitespace skipping.
    /// </summary>
    internal ref struct LuaReader
    {
        private readonly ReadOnlySpan<char> _text;
        private int _pos;

        public LuaReader(string text)
        {
            _text = text.AsSpan();
            _pos = 0;
        }

        public int Position => _pos;
        public int Length => _text.Length;
        public bool IsAtEnd => _pos >= _text.Length;
        public bool HasNext() => _pos + 1 < _text.Length;

        public char Peek() => _text[_pos];
        public char PeekAt(int offset) => _text[_pos + offset];
        public char Read() => _text[_pos++];
        public void Advance() => _pos++;

        public char CharAt(int index) => _text[index];

        public string Substring(int start, int length)
            => _text.Slice(start, length).ToString();

        public void Expect(char c)
        {
            if (IsAtEnd || _text[_pos] != c)
                throw new FormatException(
                    $"Expected '{c}' at position {_pos}, got '{(IsAtEnd ? "EOF" : _text[_pos].ToString())}'");
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

            // Ensure not part of a longer identifier
            if (_pos + keyword.Length < _text.Length && (char.IsLetterOrDigit(_text[_pos + keyword.Length]) || _text[_pos + keyword.Length] == '_'))
                return false;

            _pos += keyword.Length;
            return true;
        }

        /// <summary>
        /// Try to consume a keyword at the current position.
        /// Unlike TryConsume, this is specifically for block keyword detection
        /// during function literal skipping.
        /// </summary>
        public bool TryConsumeKeyword(string keyword)
        {
            // Must be at start of an identifier
            if (_pos > 0 && (char.IsLetterOrDigit(_text[_pos - 1]) || _text[_pos - 1] == '_'))
                return false;

            return TryConsume(keyword);
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

                // Lua comment: --
                if (c == '-' && _pos + 1 < _text.Length && _text[_pos + 1] == '-')
                {
                    // Block comment: --[[ ... ]]
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
                        // Line comment: skip to end of line
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
