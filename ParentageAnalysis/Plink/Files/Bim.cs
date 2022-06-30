using Microsoft.Data.Analysis;
using Sporbarhet.Parentage.Extensions;
using System.Globalization;

namespace Sporbarhet.Parentage.Plink;
public static partial class PlinkFiles
{
    /// <summary>
    /// The column names in a PLINK .bim file.
    /// <br/>
    /// A PLINK .bim file consists of the following six columns:
    /// <list type="number">
    /// <item>Chromosome code(either an integer, or 'X'/'Y'/'XY'/'MT'; '0' indicates unknown) or name</item>
    /// <item>Variant identifier</item>
    /// <item>Position in morgans or centimorgans(safe to use dummy value of '0')</item>
    /// <item>Base-pair coordinate(1-based; limited to 231-2)</item>
    /// <item>Allele 1 (corresponding to clear bits in .bed; usually minor)</item>
    /// <item>Allele 2 (corresponding to set bits in .bed; usually major)</item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#bim"/>)
    /// </summary>
    public static readonly string[] bimColumnNames = new[]
    {
        "Chromosome", "VariantID", "CentiMorgans", "BasePairCoordinate", "Allele1", "Allele2",
    };

    /// <summary>
    /// The column data types in a PLINK .bim file.
    /// <br/>
    /// A PLINK .bim file consists of the following six columns:
    /// <list type="number">
    /// <item>Chromosome code(either an integer, or 'X'/'Y'/'XY'/'MT'; '0' indicates unknown) or name</item>
    /// <item>Variant identifier</item>
    /// <item>Position in morgans or centimorgans(safe to use dummy value of '0')</item>
    /// <item>Base-pair coordinate(1-based; limited to 231-2)</item>
    /// <item>Allele 1 (corresponding to clear bits in .bed; usually minor)</item>
    /// <item>Allele 2 (corresponding to set bits in .bed; usually major)</item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#bim"/>)
    /// </summary>
    public static readonly Type[] bimDataTypes = new[]
    {
        //Chromosome    VariantID       CentiMorgans    BasePairCoord Allele1       Allele2
        typeof(string), typeof(string), typeof(double), typeof(long), typeof(char), typeof(char),
    };

    /// <summary>
    /// The default column separator in a PLINK .bim file.
    /// See <see cref="ReadBim(string, char, bool, int, int)"> for more details on this file format.
    /// </summary>
    public const char bimDefaultSeparator = '\t';

    /// <summary>
    /// Reads the contents of a PLINK .bim file from the given <paramref name="path"/>.
    /// <br/>
    /// A PLINK .bim file is an extended variant information file accompanying a .bed binary genotype table. It is also called an extended .map file.
    /// It is a text file with no header line, and one line per variant with the following six fields:
    /// <list type="number">
    /// <item>Chromosome code (either an integer, or 'X'/'Y'/'XY'/'MT'; '0' indicates unknown) or name</item>
    /// <item>Variant identifier</item>
    /// <item>Position in morgans or centimorgans(safe to use dummy value of '0')</item>
    /// <item>Base-pair coordinate (1-based; limited to 231-2)</item>
    /// <item>Allele 1 (corresponding to clear bits in .bed; usually minor)</item>
    /// <item>Allele 2 (corresponding to set bits in .bed; usually major)</item> 
    /// </list>
    /// Allele codes can contain more than one character. Variants with negative bp coordinates are ignored by PLINK.
    /// <br/>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#bim"/>)
    /// </summary>
    /// <param name="path">The file path to the PLINK .bim file.</param>
    /// <param name="sep">The column separator used in the file. Defaults to <see cref="bimDefaultSeparator"/>.</param>
    /// <param name="header">Specifies whether the file contains a header line or not. Defaults to <see langword="false"/>.</param>
    /// <param name="retries">The number of attempts before the method gives up on accessing the file.</param>
    /// <param name="baseRetrySleepMs">The base number of milliseconds that is waited before reattempting to access the file.</param>
    /// <returns>A <see cref="DataFrame"/> containing the information in the file.</returns>
    public static DataFrame ReadBim(string path, char sep = bimDefaultSeparator, bool header = false, int retries = 1, int baseRetrySleepMs = 50) =>
        FlowControl.WithCulture(CultureInfo.InvariantCulture, () =>
        FlowControl.WithRetries<DataFrame, FileNotFoundException>(() =>
        DataFrame.LoadCsv(path, sep, header, bimColumnNames, bimDataTypes), retries, baseRetrySleepMs));
}
