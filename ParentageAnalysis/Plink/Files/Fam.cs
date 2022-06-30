using Microsoft.Data.Analysis;
using Sporbarhet.Parentage.Extensions;

namespace Sporbarhet.Parentage.Plink;
public static partial class PlinkFiles
{
    /// <summary>
    /// The column names in a PLINK .fam file.
    /// <br/>
    /// A PLINK .fam file consists of the following six columns:
    /// <list type="number">
    /// <item>Family ID('FID')</item>
    /// <item>Within-family ID('IID'; cannot be '0')</item>
    /// <item>Within-family ID of father('0' if father isn't in dataset)</item>
    /// <item>Within-family ID of mother ('0' if mother isn't in dataset)</item>
    /// <item>Sex code ('1' = male, '2' = female, '0' = unknown)</item>
    /// <item>Phenotype value('1' = control, '2' = case, '-9'/'0'/non-numeric = missing data if case/control). See also <seealso cref=" Enums.Phenotype"/></item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#fam"/>)
    /// </summary>
    public static readonly string[] famColumnNames = new[]
    {
        "FID", "IID", "SireID", "DamID", "Sex", "Phenotype",
    };

    /// <summary>
    /// The column data types in a PLINK .fam file.
    /// <br/>
    /// A PLINK .fam file consists of the following six columns:
    /// <list type="number">
    /// <item>Family ID('FID')</item>
    /// <item>Within-family ID('IID'; cannot be '0')</item>
    /// <item>Within-family ID of father('0' if father isn't in dataset)</item>
    /// <item>Within-family ID of mother ('0' if mother isn't in dataset)</item>
    /// <item>Sex code ('1' = male, '2' = female, '0' = unknown)</item>
    /// <item>Phenotype value('1' = control, '2' = case, '-9'/'0'/non-numeric = missing data if case/control). See also <seealso cref=" Enums.Phenotype"/></item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#fam"/>)
    /// </summary>
    public static readonly Type[] famDataTypes = new[]
    {
        //FID       IID         SireID      DamID       Sex          Phenotype
        typeof(ID), typeof(ID), typeof(ID), typeof(ID), typeof(Sex), typeof(int),
    };

    /// <summary>
    /// The default column separator for PLINK .fam files.
    /// See <see cref="ReadFam(string, char, bool, int, int)"> for more details on this file format.
    /// </summary>
    public const char famDefaultSeparator = ' ';

    /// <summary>
    /// Reads the contents of a PLINK .fam file from the given <paramref name="path"/>.
    /// <br/>
    /// A PLINK .fam file is a sample information file accompanying a .bed binary genotype table.
    /// It is formatted a text file with no header line, and one line per sample with the following six fields:
    /// <list type="number">
    /// <item>Family ID('FID')</item>
    /// <item>Within-family ID('IID'; cannot be '0')</item>
    /// <item>Within-family ID of father('0' if father isn't in dataset)</item>
    /// <item>Within-family ID of mother ('0' if mother isn't in dataset)</item>
    /// <item>Sex code ('1' = male, '2' = female, '0' = unknown)</item>
    /// <item>Phenotype value('1' = control, '2' = case, '-9'/'0'/non-numeric = missing data if case/control). See also <seealso cref=" Enums.Phenotype"/></item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#fam"/>)
    /// </summary>
    /// <param name="path">The file path to the PLINK .fam file.</param>
    /// <param name="sep">The column separator used in the file. Defaults to <see cref="famDefaultSeparator"/>.</param>
    /// <param name="header">Specifies whether the file contains a header line or not. Defaults to <see langword="false"/>.</param>
    /// <param name="retries">The number of attempts before the method gives up on accessing the file.</param>
    /// <param name="baseRetrySleepMs">The base number of milliseconds that is waited before reattempting to access the file.</param>
    /// <returns>A <see cref="DataFrame"/> containing the information in the file.</returns>
    public static DataFrame ReadFam(string path, char sep = famDefaultSeparator, bool header = false, int retries = 1, int baseRetrySleepMs = 50) =>
        FlowControl.WithRetries<DataFrame, FileNotFoundException>(() =>
        DataFrame.LoadCsv(path, sep, header, famColumnNames, famDataTypes), retries, baseRetrySleepMs); //TODO: throw exception if less columns than expected. Turns out different column separating characters are valid (' ', '\t')

    /// <summary>
    /// Reads sample id and sex information from a PLINK .fam file at the given <paramref name="path"/>.
    /// <br/>
    /// A PLINK .fam file is a sample information file accompanying a .bed binary genotype table.
    /// It is formatted a text file with no header line, and one line per sample with the following six fields:
    /// <list type="number">
    /// <item>Family ID('FID')</item>
    /// <item>Within-family ID('IID'; cannot be '0')</item>
    /// <item>Within-family ID of father('0' if father isn't in dataset)</item>
    /// <item>Within-family ID of mother ('0' if mother isn't in dataset)</item>
    /// <item>Sex code ('1' = male, '2' = female, '0' = unknown)</item>
    /// <item>Phenotype value('1' = control, '2' = case, '-9'/'0'/non-numeric = missing data if case/control)</item>
    /// </list>
    /// (Taken from <seealso href="https://www.cog-genomics.org/plink/1.9/formats#fam"/>)
    /// </summary>
    /// <param name="path">The file path to the PLINK .fam file.</param>
    /// <param name="sep">The column separator used in the file. Defaults to <see cref="famDefaultSeparator"/>.</param>
    /// <param name="header">Specifies whether the file contains a header line or not. Defaults to <see langword="false"/>.</param>
    /// <param name="retries">The number of attempts before the method gives up on accessing the file.</param>
    /// <param name="baseRetrySleepMs">The base number of milliseconds that is waited before reattempting to access the file.</param>
    /// <returns>A correspondence of sample ids and their sexes, read from the PLINK .fam file.</returns>
    public static Dictionary<ID, Sex> ReadFamSexes(string path, char sep = famDefaultSeparator, bool header = false, int retries = 1, int baseRetrySleepMs = 50)
    {
        var df = ReadFam(path, sep, header, retries, baseRetrySleepMs);
        var iids = (StringDataFrameColumn)df["IID"];
        var sexesCol = (PrimitiveDataFrameColumn<Sex>)df["Sex"];

        var sexes = new Dictionary<ID, Sex>((int)df.Rows.Count);
        for (long i = 0; i < df.Rows.Count; i++)
            sexes[iids[i]] = sexesCol[i] is Sex sex ? sexLookup.GetValueOrDefault(sex, '0') : '0';

        return sexes;
    }
}
