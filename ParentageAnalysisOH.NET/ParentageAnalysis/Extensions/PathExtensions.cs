namespace Sporbarhet.Parentage.Extensions;

/// <summary>
/// A collection of general extension methods relating to path handling.
/// </summary>
static class PathExtensions
{
    private static readonly char[] directorySeparatorChars = new[]{Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};

    /// <summary>
    /// Splits a path into a folder and a file part.
    /// If no directory separator character is present, it will be assumed that the entire string is a file name.
    /// In this case "." will be returned as the folder part.
    /// </summary>
    /// <param name="path">A path to split.</param>
    /// <returns>The folder and file part of the path as strings.</returns>
    public static (string FolderPart, string FilePart) SplitFolderAndFilePart(string path)
    {
        int ind = path.LastIndexOfAny(directorySeparatorChars);
        return ind == -1 ? (".", path) : (path[..ind], path[(ind + 1)..]);
    }
}
