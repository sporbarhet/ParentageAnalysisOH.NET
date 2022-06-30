using Sporbarhet.Parentage.Extensions;
using System.Text;

namespace Sporbarhet.Parentage.Plink.Enums;

/// <summary>
/// Describes the files in a file set.
/// A file set is addressed by a file path without its extension. We dub these paths as <i>stubs</i>.
/// A <see cref="FileType"/> instance related to that stub then describes which files are in it.
/// Usually, these files are identified by a unique file extension. See <see cref="FileTypes.ExtensionToFileType"/> for this correspondence.
/// <br/>
/// <example>
/// For instance, assume that  there exists files "f.map" and "f.ped" in the folder "asd asd". Then we have a file set consisting of a map and ped file with the stub "asd asd/f".
/// In this case, the correct <see cref="FileType"/> would be <c><see cref="Map"/> | <see cref="Ped"/></c>
/// </example>
/// </summary>
/// <remarks>
/// For an exhaustive list of file types produced by PLINK, see <see href="https://www.cog-genomics.org/plink/1.9/output"/>.
/// </remarks>
[Flags]
public enum FileType : ulong
{
    None = default,
    /// <summary>PLINK binary biallelic genotype table.</summary>
    Bed = 1ul << 0,
    /// <summary>PLINK variant information file, part of binary file set.</summary>
    Bim = 1ul << 1,
    /// <summary>PLINK sample information text file, part of binary file set.</summary>
    Fam = 1ul << 2,
    /// <summary>PLINK/Merlin/Haploview text sample + genome data file.</summary>
    Ped = 1ul << 3,
    /// <summary>PLINK text file set variant information file.</summary>
    Map = 1ul << 4,
    /// <summary></summary>
    Pgen = 1ul << 5,
    /// <summary></summary>
    Pvar = 1ul << 6,
    /// <summary></summary>
    Psam = 1ul << 7,
    /// <summary>Method-of-moments F coefficient estimates for heterozygosity of samples. --het</summary>
    Het = 1ul << 8,
    /// <summary>Additive + dominant component file. --recode {A,AD}</summary>
    Raw = 1ul << 9,
    /// <summary>Basic allele frequency report. --freq</summary>
    Frq = 1ul << 10,
    /// <summary></summary>
    Pheno = 1ul << 11,
    /// <summary>Case/control strand inconsistency report. --flip-scan</summary>
    FlipScan = 1ul << 12,
    /// <summary>List of samples with ambiguous sex codes.</summary>
    Nosex = 1ul << 13,
    /// <summary>List of variants with more than two alleles. --bmerge</summary>
    Missnp = 1ul << 14,
    /// <summary>IDs of samples excluded by --mind.</summary>
    Irem = 1ul << 15,
    /// <summary>IDs of samples to exclude due to poor heterozygosity.</summary>
    /// <remarks>This is not an official PLINK file type. We use this type in our internal heterozygosity quality control.</remarks>
    HetPoor = 1ul << 48,
    /// <summary>Comma separated values(generally not comma separated)</summary>
    Csv = 1ul << 61,
    /// <summary>Logged information about a PLINK run.</summary>
    Log = 1ul << 62,
    /// <summary>A file with an extension that is not recognized.</summary>
    Unrecognized = 1ul << 63,

    /// <summary>PLINK text file set consisting of a .ped and .map file.</summary>
    PedMap = Ped | Map,
    /// <summary>PLINK binary file set consisting of a .bed, .bim and .fam file.</summary>
    Binary = Bed | Bim | Fam,
    /// <summary>PLINK2 binary file set consisting of a .pgen, .pvar and .psam file.</summary>
    Pfile = Pgen | Pvar | Psam,
    /// <summary>PLINK2 binary file set consisting of a .pgen, .bim and .fam file.</summary>
    Bpfile = Pgen | Bim | Fam,

    /// <summary>
    /// Symbolizes that a file is present, but without specifying a file type.
    /// Used in our PLINK output commands where the type of the output is inferred from other arguments in the command.
    /// </summary>
    Anonymous = ~None
}


/// <summary>
/// Methods relating to translation between file types, paths and files.
/// </summary>
public static class FileTypes
{
    /// <summary>
    /// Dictionary relating file extensions to file types.
    /// See also <seealso cref="GetFileTypes(string)"/>
    /// </summary>
    /// <remarks>This dictionary is case-insensitive.</remarks>
    public static readonly Dictionary<string, FileType> ExtensionToFileType = new(19, StringComparer.OrdinalIgnoreCase)
    {
        [".log"] = FileType.Log,
        [".bed"] = FileType.Bed,
        [".bim"] = FileType.Bim,
        [".fam"] = FileType.Fam,
        [".ped"] = FileType.Ped,
        [".map"] = FileType.Map,
        [".pgen"] = FileType.Pgen,
        [".pvar"] = FileType.Pvar,
        [".psam"] = FileType.Psam,
        [".het"] = FileType.Het,
        [".raw"] = FileType.Raw,
        [".frq"] = FileType.Frq,
        [".pheno"] = FileType.Pheno,
        [".flipscan"] = FileType.FlipScan,
        [".nosex"] = FileType.Nosex,
        [".missnp"] = FileType.Missnp,
        [".irem"] = FileType.Irem,
        [".hetpoor"] = FileType.HetPoor,
        [".csv"] = FileType.Csv,
    };

    /// <summary>
    /// Determines the file types present in the file set at the specified stub.
    /// </summary>
    /// <param name="stub">The stub to the file set. A stub is a file path without an extension.</param>
    /// <returns>The file types in the specified file set.</returns>
    public static FileType GetFileTypes(string stub)
    {
        (string folder, string fileName) = PathExtensions.SplitFolderAndFilePart(stub);
        string[] files = Directory.GetFiles(folder, fileName + ".*", SearchOption.TopDirectoryOnly);
        var type = FileType.None;

        string stubName = Path.GetFileName(stub);

        foreach (string file in files)
        {
            string extension = Path.GetFileName(file)[stubName.Length..];
            type |= ExtensionToFileType.GetValueOrDefault(extension, FileType.Unrecognized);
        }
        return type;
    }

    /// <summary>
    /// Determines the file types present in the specified file set.
    /// </summary>
    /// <param name="files">The files in the file set.</param>
    /// <returns>The file types in the specified file set.</returns>
    public static FileType GetFileTypes(IEnumerable<string> files)
    {
        var type = FileType.None;
        foreach (string file in files)
        {
            string extension = Path.GetExtension(file);
            type |= ExtensionToFileType.GetValueOrDefault(extension, FileType.Unrecognized);
        }
        return type;
    }


    /// <summary>
    /// A dictionary relating file types to PLINK output flags.
    /// See also <seealso cref="GetPlinkOutFlags(string, FileType)"/>.
    /// </summary>
    /// <remark>The flags in this dictionary are "--" prefixed and are padded by whitespace on the left.</remark>
    static readonly Dictionary<FileType, string> outFileTypeToPlinkFlag = new(10)
    {
        // File sets first
        [FileType.Pfile] = " --mape-pgen",
        [FileType.Bpfile] = " --make-bpgen",
        [FileType.Binary] = " --make-bed",
        [FileType.PedMap] = " --recode",
        // Individual files
        [FileType.Pvar] = " --make-just-pvar",
        [FileType.Bim] = " --make-just-bim",
        [FileType.Fam] = " --make-just-fam",
        [FileType.Het] = " --het",
        [FileType.Raw] = " --recode A", //There is also an alternative output mode --recode AD
        [FileType.Frq] = " --freq",
        [FileType.FlipScan] = " --flip-scan",
    };

    /// <summary>
    /// Constructs the appropriate PLINK flag string for outputting a file set of the given <paramref name="fileType"/>.
    /// </summary>
    /// <param name="outStub">The stub to the output file set. A stub is a file path without an extension.</param>
    /// <param name="fileType">The types of the file set to output.</param>
    /// <returns>The PLINK flag string for outputting a file set of the given file type.</returns>
    /// <exception cref="ArgumentException">Thrown when the file type contains unsupported flags.</exception>
    public static string GetPlinkOutFlags(string outStub, FileType fileType)
    {
        if (fileType == FileType.None)
            return "";
        if (fileType == FileType.Anonymous)
            return $"--out \"{outStub}\"";


        var sb = new StringBuilder($"--out \"{outStub}\"");
        foreach (var kv in outFileTypeToPlinkFlag.Where(kv => fileType.HasFlag(kv.Key)))
        {
            fileType &= ~kv.Key;
            sb.Append(kv.Value);
        }

        if ((fileType & ~FileType.Unrecognized) != default)
            throw new ArgumentException($"The file type contains unrecognized flags: {fileType}.", nameof(fileType));

        return sb.ToString();
    }


    /// <summary>
    /// A dictionary relating file types to PLINK input flags.
    /// See also <seealso cref="GetPlinkInFlags(string, FileType)"/>.
    /// </summary>
    /// <remark>
    /// The flags in this dictionary are missing the "--" prefix and are not padded.
    /// </remark>
    static readonly Dictionary<FileType, string> inFileTypeToPlinkFlag = new(12)
    {
        // File sets first
        [FileType.Pfile] = "pfile",
        [FileType.Bpfile] = "bpfile",
        [FileType.Binary] = "bfile",
        [FileType.PedMap] = "pedmap", // forward compatible with PLINK2
        // Individual files
        [FileType.Pgen] = "pgen",
        [FileType.Pvar] = "pvar",
        [FileType.Psam] = "psam",
        [FileType.Bed] = "bed",
        [FileType.Bim] = "bim",
        [FileType.Fam] = "fam",
        [FileType.Ped] = "ped",
        [FileType.Map] = "map",
    };

    /// <summary>
    /// Constructs an appropriate PLINK flag string for inputting a file set of the given <paramref name="fileType"/>.
    /// </summary>
    /// <param name="inStub">The stub to the input file set. A stub is a file path without an extension.</param>
    /// <param name="fileType">The types of the file set to input.</param>
    /// <returns>The PLINK flag string for inputting a file set of the given file type.</returns>
    public static string GetPlinkInFlags(string inStub, out FileType fileType)
    {
        fileType = GetFileTypes(inStub);
        return GetPlinkInFlags(inStub, fileType);
    }

    /// <summary>
    /// Constructs an appropriate PLINK flag string for inputting a file set of the given <paramref name="fileType"/>.
    /// </summary>
    /// <param name="inStub">The stub to the input file set. A stub is a file path without an extension.</param>
    /// <param name="fileType">The types of the file set to input.</param>
    /// <returns>The PLINK flag string for inputting a file set of the given file type.</returns>
    /// <exception cref="ArgumentException">Thrown when the file type contains unsupported flags.</exception>
    public static string GetPlinkInFlags(string inStub, FileType fileType)
    {
        if (fileType == FileType.None)
            return "";
        if (fileType == FileType.Anonymous)
            throw new ArgumentException("The anonymous file type is not supported.", nameof(fileType));

        string res = string.Join(' ', inFileTypeToPlinkFlag.Where(kv => fileType.HasFlag(kv.Key)).Select(kv => { fileType &= ~kv.Key; return $"--{kv.Value} \"{inStub}\""; }));

        return res;
    }


    /// <summary>
    /// Determines whether <paramref name="this"/> <see cref="FileType"/> contains the specified file <paramref name="types"/>.
    /// <br/>
    /// <example>
    /// For instance, to check if a file set contains a bed/bim/fam file triple, you can call this method with <c><paramref name="types"/> = <see cref="FileType.Binary"/></c>
    /// </example>
    /// </summary>
    /// <param name="this">This file set.</param>
    /// <param name="types">The types to we want to know if <paramref name="this"/> contains.</param>
    /// <returns>Whether <paramref name="this"/> <see cref="FileType"/> contains the specified file <paramref name="types"/>.</returns>
    public static bool Contains(this FileType @this, FileType types) => (@this & types) == types && @this != FileType.Anonymous;

    /// <summary>
    /// Determines whether the file set at <paramref name="stub"/> contains the file <paramref name="types"/>.
    /// <br/>
    /// <example>
    /// For instance, to check if a file set contains a bed/bim/fam file triple, you can call this method with <c><paramref name="types"/> = <see cref="FileType.Binary"/></c>
    /// </example>
    /// </summary>
    /// <param name="stub">The stub to the file set. A stub is a file path without an extension.</param>
    /// <param name="types">The types to we want to know if the file set contains.</param>
    /// <param name="actual">The actual <see cref="FileType"/> of the file set.</param>
    /// <returns>Whether the file set contains the specified file <paramref name="types"/>.</returns>
    public static bool Contains(string stub, FileType types, out FileType actual) => (actual = GetFileTypes(stub)).Contains(types);

    /// <inheritdoc cref="Contains(string, FileType, out FileType)"/>
    public static bool Contains(string stub, FileType types) => Contains(stub, types, out _);

    /// <summary>
    /// Determines whether <paramref name="this"/> <see cref="FileType"/> contains any of the specified file <paramref name="types"/>.
    /// <br/>
    /// <example>
    /// For instance, to check if a file set contains genotyping data, you can call this method with <c><paramref name="types"/> = <see cref="FileType.Ped"/> | <see cref="FileType.Bed"/> | <see cref="FileType.Pgen"/></c>
    /// </example>
    /// </summary>
    /// <param name="this">This file set.</param>
    /// <param name="types">The types to we want to know if <paramref name="this"/> contains.</param>
    /// <returns>Whether <paramref name="this"/> <see cref="FileType"/> contains any of the specified file <paramref name="types"/>.</returns>
    public static bool ContainsAny(this FileType @this, FileType types) => (@this & types) != default && @this != FileType.Anonymous;

    /// <summary>
    /// Determines whether the file set at <paramref name="stub"/> contains any of the file <paramref name="types"/>.
    /// <br/>
    /// <example>
    /// For instance, to check if a file set contains genotyping data, you can call this method with <c><paramref name="types"/> = <see cref="FileType.Ped"/> | <see cref="FileType.Bed"/> | <see cref="FileType.Pgen"/></c>
    /// </example>
    /// </summary>
    /// <param name="stub">The stub to the file set. A stub is a file path without an extension.</param>
    /// <param name="types">The types to we want to know if the file set contains.</param>
    /// <param name="actual">The actual <see cref="FileType"/> of the file set.</param>
    /// <returns>Whether the file set contains any of the specified file <paramref name="types"/>.</returns>
    public static bool ContainsAny(string stub, FileType types, out FileType actual) => (actual = GetFileTypes(stub)).ContainsAny(types);

    /// <inheritdoc cref="ContainsAny(string, FileType, out FileType)"/>
    public static bool ContainsAny(string stub, FileType types) => ContainsAny(stub, types, out _);
}
