using Microsoft.Data.Analysis;
using Sporbarhet.Parentage.Extensions;
using System.Globalization;

namespace Sporbarhet.Parentage.Plink;
public static partial class PlinkFiles
{
    /// <summary>
    /// The column names in a PLINK .het file.
    /// <br/>
    /// A PLINK .het file consists of the following six columns:
    /// <list type="number">
    /// <item>FID: Family ID</item>
    /// <item>IID: Within-family ID</item>
    /// <item>O(HOM): Observed number of homozygotes</item>
    /// <item>E(HOM): Expected number of homozygotes</item>
    /// <item>N(NM): Number of(nonmissing, non-monomorphic) autosomal genotype observations</item>
    /// <item>F: Method-of-moments F coefficient estimate</item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#het"/>)
    /// </summary>
    public static readonly string[] hetColumnNames = new[]
    {
        "FID", "IID", "O(HOM)", "E(HOM)", "N(NM)", "F",
    };

    /// <summary>
    /// The column data types in a PLINK .het file.
    /// <br/>
    /// A PLINK .het file consists of the following six columns:
    /// <list type="number">
    /// <item>FID: Family ID</item>
    /// <item>IID: Within-family ID</item>
    /// <item>O(HOM): Observed number of homozygotes</item>
    /// <item>E(HOM): Expected number of homozygotes</item>
    /// <item>N(NM): Number of(nonmissing, non-monomorphic) autosomal genotype observations</item>
    /// <item>F: Method-of-moments F coefficient estimate</item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#het"/>)
    /// </summary>
    public static readonly Type[] hetDataTypes = new[]
    {
        // Columns 3-5 contain integer values, but we want to do floating point arithmetic on them
        typeof(ID), typeof(ID), typeof(double), typeof(double), typeof(double), typeof(double),
    };

    /// <summary>
    /// Our default column separator for PLINK .het contents.
    /// </summary>
    /// <remarks>
    /// The PLINK .het files are usually column separated by variying amounts of whitespace to align the columns vertically.
    /// This is trimmed by the method <see cref="StringExtensions.RemoveRepeatWhiteSpace(string)"/> before parsing.
    /// </remarks>
    public const char hetSeparator = ' ';

    /// <summary>
    /// Reads heterozygosity statistics from a PLINK .het file at <paramref name="path"/>.
    /// <br/>
    /// A PLINK .het file is a text file with a header line, and one line per sample with the following six fields:
    /// <list type="number">
    /// <item>FID: Family ID</item>
    /// <item>IID: Within-family ID</item>
    /// <item>O(HOM): Observed number of homozygotes</item>
    /// <item>E(HOM): Expected number of homozygotes</item>
    /// <item>N(NM): Number of(nonmissing, non-monomorphic) autosomal genotype observations</item>
    /// <item>F: Method-of-moments F coefficient estimate</item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#het"/>)
    /// </summary>
    /// <remarks>
    /// Warning: These files may contain values written in exponential form, which are not parsed correctly by this function.
    /// </remarks>
    /// <param name="path">The file path to the PLINK .het file.</param>
    /// <param name="sep">The column separator used in the file. Defaults to <see cref="hetSeparator"/>.</param>
    /// <param name="header">Specifies whether the file contains a header line or not. Defaults to <see langword="true"/>.</param>
    /// <param name="retries">The number of attempts before the method gives up on accessing the file.</param>
    /// <param name="baseRetrySleepMs">The base number of milliseconds that is waited before reattempting to access the file.</param>
    /// <returns>A <see cref="DataFrame"/> containing the information in the .het file.</returns>
    public static DataFrame ReadHet(string path, char sep = hetSeparator, bool header = true, int retries = 1, int baseRetrySleepMs = 50) =>
        FlowControl.WithCulture(CultureInfo.InvariantCulture, () =>
        FlowControl.WithRetries<DataFrame, FileNotFoundException>(() =>
        DataFrame.LoadCsvFromString(File.ReadAllText(path).RemoveRepeatWhiteSpace(), sep, header, hetColumnNames, hetDataTypes), retries, baseRetrySleepMs));
}
