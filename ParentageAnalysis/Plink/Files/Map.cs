using Microsoft.Data.Analysis;
using Sporbarhet.Parentage.Extensions;
using System.Globalization;

namespace Sporbarhet.Parentage.Plink;
public static partial class PlinkFiles
{
    /// <summary>
    /// The column names in a PLINK .map file.
    /// <br/>
    /// A PLINK .bim file consists of the following three to four columns:
    /// <list type="number">
    /// <item>Chromosome code</item>
    /// <item>Variant identifier</item>
    /// <item>Position in morgans or centimorgans (optional; also safe to use dummy value of '0')</item>
    /// <item>Base-pair coordinate</item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#map"/>)
    /// </summary>
    public static readonly string[] mapColumnNames = new[]
    {
        "Chromosome", "VariantID", "CentiMorgans", "BasePairCoordinate"
    };

    /// <summary>
    /// The column data types in a PLINK .map file.
    /// <br/>
    /// A PLINK .bim file consists of the following three to four columns:
    /// <list type="number">
    /// <item>Chromosome code</item>
    /// <item>Variant identifier</item>
    /// <item>Position in morgans or centimorgans (optional; also safe to use dummy value of '0')</item>
    /// <item>Base-pair coordinate</item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#map"/>)
    /// </summary>
    public static readonly Type[] mapDataTypes = new[]
    {
        typeof(int), typeof(string), typeof(string), typeof(string)
    };

    /// <summary>
    /// The default column separator in a PLINK .map file.
    /// See <see cref="ReadMap(string, char, bool, int, int)"> for more details on this file format.
    /// </summary>
    public const char mapDefaultSeparator = '\t';

    /// <summary>
    /// Read the contents of a PLINK .map file from the given <see cref="path"/>.
    /// <br/>
    /// A PLINK .map file is a variant information file accompanying a PLINK .ped text pedigree + genotype table.
    /// It is a text file with no header line, and one line per variant with the following three to four fields:
    /// <list type="number">
    /// <item>Chromosome code</item>
    /// <item>Variant identifier</item>
    /// <item>Position in morgans or centimorgans (optional; also safe to use dummy value of '0')</item>
    /// <item>Base-pair coordinate</item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#map"/>)
    /// </summary>
    /// <remarks>
    /// This method only works if all four fields are present in the file. This means that the 
    /// </remarks>
    /// <param name="path">The file path to the PLINK .map file.</param>
    /// <param name="sep">The column separator used in the file. Defaults to <see cref="mapDefaultSeparator"/>.</param>
    /// <param name="header">Specifies whether the file contains a header line or not. Defaults to <see langword="false"/>.</param>
    /// <param name="retries">The number of attempts before the method gives up on accessing the file.</param>
    /// <param name="baseRetrySleepMs">The base number of milliseconds that is waited before reattempting to access the file.</param>
    /// <returns>A <see cref="DataFrame"/> containing the information in the file.</returns>
    public static DataFrame ReadMap(string path, char sep = mapDefaultSeparator, bool header = false, int retries = 1, int baseRetrySleepMs = 50) =>
        FlowControl.WithCulture(CultureInfo.InvariantCulture, () =>
        FlowControl.WithRetries<DataFrame, FileNotFoundException>(() =>
        DataFrame.LoadCsvFromString(string.Join('\n', File.ReadLines(path).SkipWhile(line => line[0] == '#')), sep, header, mapColumnNames, mapDataTypes), retries, baseRetrySleepMs));
}
