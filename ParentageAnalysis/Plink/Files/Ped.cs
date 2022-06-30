using Sporbarhet.Parentage.Extensions;

namespace Sporbarhet.Parentage.Plink;
public static partial class PlinkFiles
{
    /// <summary>
    /// The typical column separators in a PLINK .ped file.
    /// </summary>
    /// <remarks>
    /// For some reason someone decided that using spaces to separate the columns in a file format where some of the fields are internally separated by spaces was a good idea.
    /// </remarks>
    public static readonly char[] pedSeparators = new []{ ' ', '\t' };

    /// <summary>
    /// Read the sample ids (IID) from a .ped file at the given <paramref name="path"/>.
    /// <br/>
    /// A .ped file is a PLINK/MERLIN/Haploview text pedigree + genotype table, normally accompanied by a .map file.
    /// It is the original standard text format for sample pedigree information and genotype calls.
    /// It contains no header line, and one line per sample with 2V+6 fields where V is the number of variants.
    /// The first six fields are the same as those in a .fam file. The seventh and eighth fields are allele calls
    /// for the first variant in the .map file ('0' = no call); the 9th and 10th are allele calls for the second variant; and so on.
    /// </summary>
    /// <param name="path">The file path to the PLINK .ped file.</param>
    /// <param name="seps">The column separators that may be used in the file. Defaults to <see cref="pedSeparators"/>. See <see cref="StringExtensions.SeekColumn(ReadOnlySpan{char}, IReadOnlyList{char}, int, out char)"/> for details on how the separator character is identified.</param>
    /// <param name="retries">The number of attempts before the method gives up on accessing the file.</param>
    /// <param name="baseRetrySleepMs">The base number of milliseconds that is waited before reattempting to access the file.</param>
    /// <returns>The sample ids (IID) in the .ped file.</returns>
    public static List<ID> ReadPedIds(string path, IReadOnlyList<char>? seps = null, int retries = 1, int baseRetrySleepMs = 50) =>
        FlowControl.WithRetries<List<ID>, FileNotFoundException>(() =>
        {
            const int iidColumn = 1;
            char separator = default;

            var ids = new List<ID>();
            foreach (string line in File.ReadLines(path))
            {
                if (string.IsNullOrEmpty(line) || line[0] == '#') // This row is a comment - skip
                    continue;

                int idStart = separator == default
                    ? line.SeekColumn(seps ?? pedSeparators, iidColumn, out separator) // Infer separator, then continue to use that
                    : line.SeekColumn(separator, iidColumn);
                int idEnd = line.IndexOf(separator, idStart);

                ids.Add(line[idStart..idEnd]);
            }
            return ids;
        }, retries, baseRetrySleepMs);

    /// <summary>
    /// Read sample id and sex information from a PLINK .ped file at the given <paramref name="path"/>.
    /// <br/>
    /// A .ped file is a PLINK/MERLIN/Haploview text pedigree + genotype table, normally accompanied by a .map file.
    /// It is the original standard text format for sample pedigree information and genotype calls.
    /// It contains no header line, and one line per sample with 2V+6 fields where V is the number of variants.
    /// The first six fields are the same as those in a .fam file. The seventh and eighth fields are allele calls
    /// for the first variant in the .map file ('0' = no call); the 9th and 10th are allele calls for the second variant; and so on.
    /// </summary>
    /// <param name="path">The file path to the PLINK .ped file.</param>
    /// <param name="seps">The column separators that may be used in the file. Defaults to <see cref="pedSeparators"/>. See <see cref="StringExtensions.SeekColumn(ReadOnlySpan{char}, IReadOnlyList{char}, int, out char)"/> for details on how the separator character is identified.</param>
    /// <param name="retries">The number of attempts before the method gives up on accessing the file.</param>
    /// <param name="baseRetrySleepMs">The base number of milliseconds that is waited before reattempting to access the file.</param>
    /// <returns>A correspondence of sample ids (IID) and their sexes, read from the PLINK .ped file.</returns>
    public static Dictionary<ID, Sex> ReadPedSexes(string path, IReadOnlyList<char>? seps = null, int retries = 1, int baseRetrySleepMs = 50) =>
        FlowControl.WithRetries<Dictionary<ID, Sex>, FileNotFoundException>(() =>
        {
            const int iidColumn = 1, sexColumn = 6;
            char separator = default;

            var sexes = new Dictionary<ID, Sex>();
            foreach (string line in File.ReadLines(path))
            {
                if (string.IsNullOrEmpty(line) || line[0] == '#') // This row is a comment - skip
                    continue;

                int idStart = separator == default
                    ? line.SeekColumn(seps ?? pedSeparators, iidColumn, out separator) // Infer separator, then continue to use that
                    : line.SeekColumn(separator, iidColumn);
                int idEnd = line.IndexOf(separator, idStart);

                int spanSexLoc = line.AsSpan(idEnd + 1).SeekColumn(separator, sexColumn - iidColumn - 1);
                sexes[line[idStart..idEnd]] = sexLookup.GetValueOrDefault(line[spanSexLoc + idEnd + 1], '0');
            }
            return sexes;
        }, retries, baseRetrySleepMs);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genePairs">A sequence of SNPs on the form "A C G T T G C A..." separated by any character.</param>
    /// <param name="isCompoundPairs">Whether the gene pairs are in compound genotype format. The compound genotype format means that the alleles are all one character in length and that the calls are not separated by a character.
    /// <example>E.g. we have "AC GT TG CA..." instead of "A C G T T G C A...".</example></param>
    /// <returns>Whether all pairs are in alphabetic order. I.e. the sequence does not contain "C A", "G A", "T A", "G C", "T C", or "T G".</returns>
    private static bool IsNormalizedGenePairs(ReadOnlySpan<char> genePairs, bool isCompoundPairs = false)
    {
        int step = isCompoundPairs ? 3 : 4;
        int diff = isCompoundPairs ? 1 : 2;
        for (int i = 0; i < genePairs.Length - step + 1; i += step)
            if (genePairs[i] > genePairs[i + diff])
                return false;

        return true;
    }

    public static bool IsNormalizedPed(string path)
    {
        char sep = default;
        bool isCompoundPairs = false; //TODO
        //PERF: this can be optimized by buffering lines and parsing in parallell. See ReadRaw for an example pattern
        foreach (string line in File.ReadLines(path))
        {
            if (string.IsNullOrEmpty(line) || line[0] == '#')
                continue;

            int loc = sep == default ? line.SeekColumn(pedSeparators, 6, out sep) : line.SeekColumn(sep, 6);

            if (!IsNormalizedGenePairs(line.AsSpan(loc), isCompoundPairs))
                return false;
        }
        return true;
    }

    /// <summary>
    /// </summary>
    /// <param name="genePairs"></param>
    /// <param name="isCompoundPairs">Whether the gene pairs are in compound genotype format. The compound genotype format means that the alleles are all one character in length and that the calls are not separated by a character.
    /// <example>E.g. we have "AC GT TG CA..." instead of "A C G T T G C A...".</example></param>
    /// <returns></returns>
    public static void NormalizeGenePairs(Span<char> genePairs, bool isCompoundPairs = false)
    {
        int step = isCompoundPairs ? 3 : 4;
        int diff = isCompoundPairs ? 1 : 2;

        for (int i = 0; i < genePairs.Length - step + 1; i += step)
            if (genePairs[i] > genePairs[i + diff]) // Alphabetic ordering
                (genePairs[i], genePairs[i + diff]) = (genePairs[i + diff], genePairs[i]);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="row"></param>
    /// <param name="isCompoundPairs">Whether the gene pairs are in compound genotype format. The compound genotype format means that the alleles are all one character in length and that the calls are not separated by a character.
    /// <example>E.g. we have "AC GT TG CA..." instead of "A C G T T G C A...".</example></param>
    /// <returns></returns>
    public static string NormalizePedRow(string row, bool isCompoundPairs = false)
    {
        if (string.IsNullOrEmpty(row) || row[0] == '#')
            return row;

        char[] chars = row.ToCharArray();
        NormalizeGenePairs(chars.AsSpan(row.SeekColumn(pedSeparators, 6, out _)), isCompoundPairs);
        return new string(chars);
    }

    public static void NormalizePed(string pedPath, string unnormalizedSuffix = "_unnormalized")
    {
        string unnormalizedPath = pedPath + unnormalizedSuffix;
        if (File.Exists(unnormalizedPath))
            File.Delete(unnormalizedPath);

        bool isCompoundPairs = false; //TODO

        File.Move(pedPath, unnormalizedPath);
        //PERF: this can be optimized by buffering lines and parsing in parallell. See ReadRaw for an example pattern
        File.WriteAllLines(pedPath, File.ReadLines(unnormalizedPath).Select(row => NormalizePedRow(row, isCompoundPairs)));
    }
}
