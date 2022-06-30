
using Microsoft.Data.Analysis;
using Sporbarhet.Parentage.Extensions;
using Sporbarhet.Parentage.Plink.Enums;
using System.Globalization;

namespace Sporbarhet.Parentage.Plink;

/// <summary>
/// A collection of methods for handling table formatted text files produced by PLINK.
/// </summary>
public static partial class PlinkFiles
{
    public static (double Lower, double Upper) CalculateHeterozygosityBounds(DataFrame het, double stdDevs = 6)
    {
        var pHet = 1d - het["O(HOM)"] / het["N(NM)"];

        // Add pHET column to input dataframe
        het["pHET"] = pHet;

        double mean = pHet.Mean();
        double stdDev = pHet.StdDev(mean);

        double lowerH = mean - stdDevs * stdDev;
        double upperH = mean + stdDevs * stdDev;
        return (lowerH, upperH);
    }

    public static DataFrame ReadPedigree(string filePath, char separator = ' ', bool header = true)
    => FlowControl.WithCulture(CultureInfo.InvariantCulture, () => DataFrame.LoadCsv(filePath, separator, header));

    /// <summary>
    /// Generic method for reading PLINK files as dataframes. Supports reading bim, fam, map, het files and generic files in a csv format.
    /// </summary>
    /// <param name="inFile"></param>
    /// <param name="separator">Only used if the filetype is csv or not recognized.</param>
    /// <param name="header">Only used if the filetype is csv or not recognized.</param>
    /// <param name="columnNames">Only used if the filetype is csv or not recognized.</param>
    /// <param name="dataTypes">Only used if the filetype is csv or not recognized.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static DataFrame ReadDataFrame(string inFile, char separator = ',', bool header = true, string[]? columnNames = null, Type[]? dataTypes = null) => Path.GetExtension(inFile).ToLowerInvariant() switch
    {
        ".bim" => ReadBim(inFile),
        ".fam" => ReadFam(inFile),
        ".map" => ReadMap(inFile),
        ".het" => ReadHet(inFile),
        _ => DataFrame.LoadCsv(inFile, separator, header, columnNames, dataTypes)
    };


    public static IEnumerable<ID> ReadIds(string path, out FileType fileType)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension))
        {
            fileType = FileTypes.GetFileTypes(path);
            //Do not infer .csv extension, as this file format is too generic to assume that the files are correct.
            if (fileType.HasFlag(FileType.Fam))
                path += extension = ".fam";
            else if (fileType.HasFlag(FileType.Ped))
                path += extension = ".ped";
            else if (fileType.HasFlag(FileType.Raw))
                path += extension = ".raw";
            else
                throw new FileNotFoundException(path);
        }
        else
            fileType = FileTypes.ExtensionToFileType.GetValueOrDefault(extension, FileType.Unrecognized);

        return extension switch
        {
            ".fam" => ((StringDataFrameColumn)ReadFam(path)["IID"]),
            ".ped" => ReadPedIds(path),
            ".raw" => ReadRawMetadata(path).Ids,
            _ => throw new NotSupportedException($"Reading id data from the specified file format ({extension}) is not supported.")
        };
    }

    public static IEnumerable<ID> ReadIds(string idPath) => ReadIds(idPath, out _);


    public static readonly string[] parentsColumnNames = new string[]
    {
        "ID", "Sex",
    };
    public static readonly Type[] parentsDataTypes = new Type[]
    {
        typeof(ID), typeof(Sex),
    };
    public const char parentsSeparator = ',';

    public static Dictionary<ID, Sex> ReadCsvSexes(string path, char separator = parentsSeparator)
    {
        // parental data with IDs and Sex of parent
        var dfParents = DataFrame.LoadCsv(path, separator, true, parentsColumnNames, parentsDataTypes);
        return ((StringDataFrameColumn)dfParents["ID"]).Zip((PrimitiveDataFrameColumn<Sex>)dfParents["Sex"]).ToDictionary(kd => kd.First, kd => sexLookup.GetValueOrDefault(kd.Second ?? default, '0'));
    }

    public static Dictionary<ID, Sex> ReadSexes(string path, out FileType parentFileType)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension))
        {
            parentFileType = FileTypes.GetFileTypes(path);
            //Do NOT infer .csv file, as this file format is to generic to assume that the files are correct.
            if (parentFileType.HasFlag(FileType.Fam))
                path += extension = ".fam";
            else if (parentFileType.HasFlag(FileType.Ped))
                path += extension = ".ped";
            else if (parentFileType.HasFlag(FileType.Raw))
                path += extension = ".raw";
            else
                throw new FileNotFoundException(path);
        }
        else
            parentFileType = FileTypes.ExtensionToFileType.GetValueOrDefault(extension, FileType.Unrecognized);

        return extension switch
        {
            ".csv" => ReadCsvSexes(path),
            ".fam" => ReadFamSexes(path),
            ".ped" => ReadPedSexes(path),
            ".raw" => ReadRawSexes(path),
            _ => throw new NotSupportedException($"Reading parent data from the specified file format ({extension}) is not supported.")
        };
    }

    public static Dictionary<ID, Sex> ReadSexes(string path) => ReadSexes(path, out _);

    public static void WriteSexes(Dictionary<ID, Sex> sexes, string outPath, char separator = ',', bool header = true)
        => DataFrame.WriteCsv(new DataFrame(DataFrameColumn.Create("ID", sexes.Keys), DataFrameColumn.Create("Sex", sexes.Values)), outPath, separator, header, cultureInfo: CultureInfo.InvariantCulture);


    public static readonly Dictionary<char, Sex> sexLookup = new(6)
    {
        ['1'] = 'M', ['M'] = 'M', ['m'] = 'M',
        ['2'] = 'F', ['F'] = 'F', ['f'] = 'F'
    };
}

