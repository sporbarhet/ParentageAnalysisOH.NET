using Sporbarhet.Parentage.Extensions;

namespace Sporbarhet.Parentage.Plink;

/// <summary>
/// Miscellaneous methods tha
/// </summary>
public static class PlinkUtility
{
    /// <summary>
    /// File extensions of output information files from PLINK that are not part of gene data sets, ped/map, bed/bim/fam etc.
    /// </summary>
    private static readonly IReadOnlySet<string> PlinkInfoFileExtensions = new HashSet<string>(4, StringComparer.OrdinalIgnoreCase) { ".csv", ".irem", ".log", ".missnp", };

    /// <summary>
    /// Delete files except PLINK information files from the file system in the same directory and with the same file name without exception as the supplied <paramref name="stub"/> path.
    /// </summary>
    /// <param name="stub">The file path without file extension.</param>
    public static void DeleteFilesExceptInfo(string stub)
    {
        (string folder, string fileNameStub) = PathExtensions.SplitFolderAndFilePart(stub);
        Directory.GetFiles(folder, fileNameStub + ".*", SearchOption.TopDirectoryOnly)
            .Where(path => !PlinkInfoFileExtensions.Contains(Path.GetExtension(path)))
            .ForEach(File.Delete);
    }


    /// <summary>
    /// Add the supplied <paramref name="argument"/> to the supplied <paramref name="argumentLine"/>.
    /// </summary>
    /// <param name="argumentLine">The line with arguments to add the argument to.</param>
    /// <param name="argument">The argument to add.</param>
    private static string AddArgument(string argumentLine, string argument)
    {
        if (!argumentLine.EndsWith(" ") && 0 < argumentLine.Length)
        {
            argumentLine += " ";
        }

        argumentLine += argument;

        return argumentLine;
    }

    /// <summary>
    /// Add an argument specifying the chromosome set to the supplied <paramref name="argumentLine"/>.
    /// </summary>
    /// <param name="chromosomeSet"></param>
    /// <returns>The augmented argument line.</returns>
    /// <inheritdoc cref="AddArgument(string, string)"/>
    public static string AddChromosomeSetArgument(this string argumentLine, int chromosomeSet) => AddArgument(argumentLine, $"--chr-set {chromosomeSet}");


}
