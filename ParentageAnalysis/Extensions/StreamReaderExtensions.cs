namespace Sporbarhet.Parentage.Extensions;
internal static class StreamReaderExtensions
{
    public static int ReadLine(this StreamReader sr, char[] buffer, int index = 0)
    {
        int ic;
        while ((ic = sr.Read()) is not (-1) and not '\n')
            buffer[index++] = (char)ic;

        return index;
    }
}
