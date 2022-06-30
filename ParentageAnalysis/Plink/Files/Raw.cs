using Sporbarhet.Parentage.BitCollections;
using Sporbarhet.Parentage.Extensions;
using Sporbarhet.Parentage.Plink.Enums;
using System.Collections.Concurrent;

namespace Sporbarhet.Parentage.Plink;
public static partial class PlinkFiles
{
    public const char rawSeparator = ' ';

    private static (BitArrayL32 AA, BitArrayL32 BB) ParseRawZygositiesPair(ReadOnlySpan<char> values, int count, char separator = ' ')
    {
        if (count == 0)
            return values.Length == 0 ? (BitArrayL32.Empty, BitArrayL32.Empty) : throw new Exception();//TODO

        int pos = 0;
        BitArrayL32 AA = new(count), BB = new(count);
        for (int j = 0; j < count; j++)
        {
            // Scan until we find the next separator
            while (values[pos++] != separator)
                ;
            // Save the first character of the entry. We interpret all entries which are not 0, 1 or 2 as "NA".
            char zyg = values[pos++];

            if (zyg == '0')
                AA.SetTrue(j);
            else if (zyg == '2')
                BB.SetTrue(j);
        }

        // Scan if there are more columns
        while (pos < values.Length && values[pos++] != separator)
            ;
        if (pos < values.Length)
            throw new FormatException($"Row contains more columns than expected.");


        return (AA, BB);
    }


    private static Zygosity[] ParseRawZygosities(ReadOnlySpan<char> values, int count, char separator = ' ')
    {
        if (count == 0)
            return values.Length == 0 ? Array.Empty<Zygosity>() : throw new Exception();//TODO

        int pos = 0;
        var zygosities = new Zygosity[count];
        for (int j = 0; j < count; j++)
        {
            // Scan until we find the next separator
            while (values[pos++] != separator)
                ;
            // Save the first character of the entry. We interpret all entries which are not 0, 1 or 2 as "NA".
            byte bzyg = (byte)(values[pos++] - '0');
            zygosities[j] = (Zygosity)Math.Min(bzyg, (byte)3);
        }
        // Scan if there are more columns
        while (pos < values.Length && values[pos++] != separator)
            ;
        if (pos < values.Length)
            throw new FormatException($"Row contains more columns than expected.");

        return zygosities;
    }

    /// <summary>
    /// Reads raw data efficiently to a dictionary of zygosities.
    /// We avoid translating from string data to any other data type by simply storing the first character of each entry.
    /// Hence the values '0', '1' and '2' should be interpreted as 0, 1, and 2 respectively.
    /// All other values, e.g. 'N', should be interpreted as unavailable/NA.
    /// </summary>
    /// <param name="rawFilePath"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    /// <exception cref="FormatException">Thrown if the file has an unexpected format.</exception>
    public static (Dictionary<string, Zygosity[]> Zygosities, (string Id, char Allele)[] Variants) ReadRaw(string rawFilePath, IEnumerable<ID>? idsToRead = null, char separator = rawSeparator, int retries = 1, int baseRetrySleepMs = 50, int nThreads = 3)
        => ReadRawWithParser(rawFilePath, ParseRawZygosities, idsToRead, separator, retries, baseRetrySleepMs, nThreads);

    /// <summary>
    /// Reads raw data efficiently to a dictionary of zygosities.
    /// We avoid translating from string data to any other data type by simply storing the first character of each entry.
    /// Hence the values '0', '1' and '2' should be interpreted as 0, 1, and 2 respectively.
    /// All other values, e.g. 'N', should be interpreted as unavailable/NA.
    /// </summary>
    /// <param name="rawFilePath"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    /// <exception cref="FormatException">Thrown if the file has an unexpected format.</exception>
    public static (Dictionary<string, (BitArrayL32 AA, BitArrayL32 BB)> Zygosities, (string Id, char Allele)[] Variants) ReadRawPair(string rawFilePath, IEnumerable<ID>? idsToRead = null, char separator = rawSeparator, int retries = 1, int baseRetrySleepMs = 50, int nThreads = 3)
        => ReadRawWithParser(rawFilePath, ParseRawZygositiesPair, idsToRead, separator, retries, baseRetrySleepMs, nThreads);

    /// <summary>
    /// Reads raw data efficiently to a dictionary of zygosities.
    /// We avoid translating from string data to any other data type by simply storing the first character of each entry.
    /// Hence the values '0', '1' and '2' should be interpreted as 0, 1, and 2 respectively.
    /// All other values, e.g. 'N', should be interpreted as unavailable/NA.
    /// </summary>
    /// <param name="rawFilePath"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    /// <exception cref="FormatException">Thrown if the file has an unexpected format.</exception>
    public static (IEnumerable<ID> Ids, (string Id, char Allele)[] Variants) ReadRawMetadata(string rawFilePath, char separator = rawSeparator, int retries = 1, int baseRetrySleepMs = 50, int nThreads = 1)
    {
        var (DummyZygosities, Variants) = ReadRawWithParser(rawFilePath, (_, _, _) => default(byte), null, separator, retries, baseRetrySleepMs, nThreads);
        return (DummyZygosities.Keys, Variants);
    }

    delegate TZygs ZygosityParser<TZygs>(ReadOnlySpan<char> line, int count, char separator = ' ');

    static (Dictionary<string, TZygs> Zygosities, (string Id, char Allele)[] Variants) ReadRawWithParser<TZygs>(string rawFilePath, ZygosityParser<TZygs> parser, IEnumerable<ID>? idsToRead = null, char separator = rawSeparator, int retries = 1, int baseRetrySleepMs = 50, int nThreads = 3)
    => FlowControl.WithRetries<(Dictionary<string, TZygs>, (string, char)[]), FileNotFoundException>(() =>
    {
        const int iidColumn = 1, metaColumns = 6;

        ConcurrentDictionary<ID, TZygs>? zygosityData = null;
        (string, char)[]? variants = null;
        HashSet<ID>? idsToReadSet = null;
        char[][]? buffers = null;

        using var sr = new StreamReader(rawFilePath);
        if (nThreads < 1)
            throw new ArgumentException("Cannot parse raw file with a non-positive number of threads.", nameof(nThreads));
        nThreads = Math.Min(nThreads, Environment.ProcessorCount);

        while (true)
        {
            string? line = sr.ReadLine();
            if (line is null)
                break;
            if (line.Length == 0 || line[0] == '#') // This line is a comment. Continue to next line
                continue;

            // We're on the first line - determine column count
            int variantsStart = line.NIndexOf(separator, metaColumns - 1);

            variants = (variantsStart < 0)
            ? Array.Empty<(string, char)>()
            : line[(variantsStart + 1)..].Split(separator).Select(s => (s[..^2], s[^1])).ToArray();

            int columns = variants.Length + metaColumns;
            //columns = line.Count(c => c == separator);
            if (columns < metaColumns)
                throw new FormatException($"Raw file \"{rawFilePath}\" is missing gene data!");

            if (idsToRead is not null)
            {
                idsToReadSet = new(idsToRead);
                zygosityData = new(nThreads, idsToReadSet.Count);
            }
            else
                zygosityData = new();


            buffers = new char[nThreads][];
            for (int i = 0; i < nThreads; i++)
                buffers[i] = new char[metaColumns * 16 + variants.Length * 3]; // Worst case

            break; // We've parsed the header, now move to the data reading loop
        }

        if (zygosityData is null || variants is null || buffers is null)
            throw new FormatException($"Raw file \"{rawFilePath}\" is empty!");


        object idsToReadLock = new(), readerLock = new();

        Parallel.For(0, nThreads, i =>
        {
            while (true)
            {
                int lineLength;
                lock (readerLock)
                    lineLength = sr.ReadLine(buffers[i]);

                if (lineLength <= 0) //TODO: this looks odd
                    return;

                ReadOnlySpan<char> line = buffers[i].AsSpan(0, lineLength);
                if (line[0] == '#') // This line is a comment - skip
                    continue;

                // ignore first 6 columns except id column
                int idStart = line.SeekColumn(separator, iidColumn);
                int idEnd = line[idStart..].IndexOf(separator);
                ID id = new string(line[idStart..(idStart + idEnd)]);

                if (idsToReadSet is not null)
                {
                    if (!idsToReadSet.Contains(id))
                    {   // Miss - are we done?
                        if (idsToReadSet.Count == 0)
                            return;
                        continue;
                    }
                    // Count id
                    lock (idsToReadLock)
                        idsToReadSet.Remove(id);
                }

                // Skip metadata columns
                TZygs z;
                if (variants.Length > 0)
                {
                    int current = line.SeekColumn(separator, metaColumns) - 1;
                    z = parser(line[current..], variants.Length, separator);
                }
                else
                    z = parser(ReadOnlySpan<char>.Empty, 0, separator);

                if (!zygosityData.TryAdd(id, z))
                    throw new Exception("");//TODO
            }
        });
        return (new(zygosityData), variants);
    }, retries, baseRetrySleepMs);


    public static Dictionary<ID, Sex> ReadRawSexes(string path, IReadOnlyList<char>? separators = null)
    {
        const int iidColumn = 1, sexColumn = 4;
        char separator = default;

        var ids = new Dictionary<ID, Sex>();
        foreach (string line in File.ReadLines(path))
        {
            if (string.IsNullOrEmpty(line) || line[0] == '#') // This row is a comment - skip over
                continue;

            int idStart = separator == default
                ? line.SeekColumn(separators ?? pedSeparators, iidColumn, out separator) // Infer separator, then continue to use that
                : line.SeekColumn(separator, iidColumn);
            int idEnd = line.IndexOf(separator, idStart);

            int spanSexLoc = line.AsSpan(idEnd + 1).SeekColumn(separator, sexColumn - iidColumn - 1);
            ids[line[idStart..idEnd]] = sexLookup.GetValueOrDefault(line[spanSexLoc + idEnd + 1], '0');
        }
        return ids;
    }
}
