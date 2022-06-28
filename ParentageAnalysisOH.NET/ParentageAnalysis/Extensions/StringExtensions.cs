using Microsoft.Toolkit.Diagnostics;

namespace Sporbarhet.Parentage.Extensions;

/// <summary>
/// A collection of general extension methods for strings.
/// </summary>
static class StringExtensions
{
    /// <summary>
    /// Creates a new string in which <paramref name="input"/> is repeated <paramref name="count"/> times.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static string Repeat(this string input, int count) => string.Concat(Enumerable.Repeat(input, count));

    /// <summary>
    /// Removes repeat white space from a string. The function <see cref="char.IsWhiteSpace(char)"/> is used to determine whether a character is white space.
    /// Every contiguous segment of white space in the string is replaced by a single space character ' ', unless there occured a new line in that segment, in which case it is replaced by a single new line character '\n'.
    /// </summary>
    /// <remarks>
    /// The method will remove repeat line breaks, and "\r\n" will be replaced by '\n'.
    /// </remarks>
    /// <param name="input">The input string.</param>
    /// <returns>An equivalent string to <paramref name="input"/> with repeat white space removed.</returns>
    public static string RemoveRepeatWhiteSpace(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        char[] output = new char[input.Length];

        int current = 0;
        bool newLine = false; // Tracks whether a newline should be inserted instead of a space
        for (int i = 0; i < input.Length; i++)
            if (!char.IsWhiteSpace(input[i]))
            {
                if (current > 0 && i > 0 && char.IsWhiteSpace(input[i - 1]))
                {
                    output[current++] = newLine ? '\n' : ' ';
                    newLine = false;
                }
                output[current++] = input[i];
            }
            else
                newLine |= input[i] == '\n';

        return new string(output, 0, current);
    }

    /// <summary>
    /// Finds the index of the <paramref name="count"/>'th (0-based) occcurence of the character <paramref name="c"/> in <paramref name="input"/>.
    /// </summary>
    /// <param name="input">The input string to search through.</param>
    /// <param name="c">The character that we are looking for.</param>
    /// <param name="count">The occurence number of <paramref name="c"/> that we want the index for.</param>
    /// <returns>The index of the <paramref name="count"/>'th (0-based) occcurence of the character <paramref name="c"/> in <paramref name="input"/>, or <c>-1</c> if there aren't enough occurences of <paramref name="c"/> in <paramref name="input"/>.</returns>
    public static int NIndexOf(this ReadOnlySpan<char> input, char c, int count)
    {
        if (count < 0)
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(count), "Count must be zero or positive.");

        int i = 0;
        do
            if (input[i] == c)
                count--;
        while (count >= 0 && ++i < input.Length);

        return count < 0 ? i : -1;
    }

    /// <inheritdoc cref="NIndexOf(ReadOnlySpan{char}, char, int)"/>
    public static int NIndexOf(this string input, char c, int count) => input.AsSpan().NIndexOf(c, count);


    /// <summary>
    /// Finds the index of the first character in the <paramref name="col"/>'th column in <paramref name="input"/>, assuming columns are seperated by <paramref name="sep"/> characters.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="sep">The column separator character.</param>
    /// <param name="col">The 0-based index of the column to seek.</param>
    /// <returns>The index of the character after the <c>(<paramref name="col"/> - 1)</c>'th <paramref name="sep"/> character in <paramref name="input"/>. If the string does not have a <paramref name="col"/>'th column, <c>-1</c> is returned.</returns>
    public static int SeekColumn(this ReadOnlySpan<char> input, char sep, int col)
    {
        if (col == 0)
            return input.Length == 0 ? ThrowHelper.ThrowArgumentException<int>(nameof(input), "") /*TODO*/ : 0; // An empty string does not have a first column

        int i = input.NIndexOf(sep, col - 1);
        return i < 0 ? ThrowHelper.ThrowArgumentException<int>(nameof(input), "") /*TODO*/ : i + 1;
    }

    /// <inheritdoc cref="SeekColumn(ReadOnlySpan{char}, char, int)"/>
    public static int SeekColumn(this string input, char sep, int col) => input.AsSpan().SeekColumn(sep, col);


    /// <summary>
    /// Finds the index of the first character in the <paramref name="col"/>'th column in <paramref name="input"/>, assuming columns are seperated by one of the <paramref name="seps"/> characters.
    /// The true separator character is infered as the first character in <paramref name="input"/> that is in <paramref name="seps"/>.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="seps"></param>
    /// <param name="col">The 0-based index of the column to seek.</param>
    /// <returns>The index of the character after the <c>(<paramref name="col"/> - 1)</c>'th <paramref name="sep"/> character in <paramref name="input"/>. If the string does not have at least <c>(<paramref name="col"/> - 1)</c> <paramref name="sep"/> characters, <c>-1</c> is returned.</returns>
    public static int SeekColumn(this ReadOnlySpan<char> input, IReadOnlyList<char> seps, int col, out char sep)
    {
        sep = default;
        if (col == 0)
            return input.Length == 0 ? -1 : 0; // An empty string does not have a first column

        int i = 0;
        while (i < input.Length && !seps.Contains(input[i++]))
            ;

        if (i >= input.Length)
            return -1; // Did not find any of the separators

        sep = input[i - 1];
        int loc = input[i..].NIndexOf(sep, col - 1);
        return loc < 0 ? loc : loc + 1;
    }


    /// <inheritdoc cref="SeekColumn(ReadOnlySpan{char}, IReadOnlyList{char}, int, out char)"/>
    public static int SeekColumn(this string input, IReadOnlyList<char> seps, int col, out char sep) => input.AsSpan().SeekColumn(seps, col, out sep);


    /// <summary>
    /// Indents a block of text by the specified indent count.
    /// </summary>
    /// <remarks>
    /// The method is agnostic to whether lines are separated by "\r\n" or just "\n".
    /// </remarks>
    /// <param name="text"></param>
    /// <param name="indentCount"></param>
    /// <param name="indentChar">Optional argument to specify the character used for indentation. Defaults to ' '.</param>
    /// <returns></returns>
    public static string Indent(this string text, int indentCount, char indentChar = ' ') => new string(indentChar, indentCount) + text.Replace("\n", "\n" + new string(indentChar, indentCount));
}
