using Microsoft.Extensions.Logging;
using Sporbarhet.Parentage.Plink.Enums;
using System.Diagnostics;

namespace Sporbarhet.Parentage.Plink;


/// <summary>
/// Class interfacing with the PLINK command line tool.
/// </summary>
public class PlinkIO
{
    /// <summary>
    /// The path to the PLINK executable.
    /// </summary>
    public string PlinkPath { get; private set; }

    protected ILogger? Logger { get; init; }


    /// <summary>
    /// Creates a new <see cref="PlinkIO"/> instance with the provided <paramref name="plinkPath"/>, <paramref name="logger"/>.
    /// </summary>
    /// <param name="plinkPath">The path to the PLINK executable. If it is not given, it is assumed that the PLINK executable is available on the global path.</param>
    /// <param name="logger">The <see cref="ILogger"/> object to pass logging messages to.</param>
    public PlinkIO(string plinkPath = "plink", ILogger? logger = null)
    {
        PlinkPath = plinkPath;
        Logger = logger;
    }


    /// <summary>
    /// Creates a new PLINK process with the passed <paramref name="arguments"/>.
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns>A new PLINK process with the passed <paramref name="arguments"/>.</returns>
    private Process CreateProcess(string arguments) => new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = PlinkPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        }
    };


    /// <summary>
    /// Starts a new PLINK process with the provided <paramref name="arguments"/> and the default arguments <c>$"--silent --allow-no-sex --chr-set {ChromosomeSet}"</c>.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the PLINK process. The default arguments will be prepended.</param>
    /// <returns>A started PLINK process with the described arguments.</returns>
    private Process StartWithDefaultArguments(string arguments)
    {
        arguments = $"--silent " + arguments;
        if (Logger is not null && Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Calling PLINK with arguments: {}", arguments);
        var process = CreateProcess(arguments);
        process.Start();
        return process;
    }

    public async Task CallAsync(string arguments)
    {
        using Process proc = StartWithDefaultArguments(arguments);
        await proc.WaitForExitAsync();
    }

    /// <summary>
    /// Calls PLINK with <paramref name="arguments"/> and infers input file type.
    /// </summary>
    /// <param name="arguments"></param>
    public Task CallWithInputAsync(string inStub, out FileType inType, string arguments = "") => CallAsync(FileTypes.GetPlinkInFlags(inStub, out inType) + " " + arguments);

    /// <summary>
    /// Runs PLINK with default arguments and input file.
    /// </summary>
    /// <param name="arguments"></param>
    public Task CallWithInputAsync(string inStub, FileType inType, string arguments = "")
        => CallAsync(FileTypes.GetPlinkInFlags(inStub, inType) + " " + arguments);

    public Task CallWithOutputAsync(string outStub, FileType outType, string arguments = "")
    => CallAsync(FileTypes.GetPlinkOutFlags(outStub, outType) + " " + arguments);

    public Task CallWithAsync(string inStub, FileType inType, string outStub, FileType outType, string arguments = "")
        => CallWithInputAsync(inStub, inType, FileTypes.GetPlinkOutFlags(outStub, outType) + " " + arguments);

    public Task CallWithAsync(string inStub, out FileType inType, string outStub, FileType outputType, string arguments = "")
        => CallWithInputAsync(inStub, out inType, FileTypes.GetPlinkOutFlags(outStub, outputType) + " " + arguments);


    public async Task CallWithOutputAndCheckAsync(string outStub, FileType outType, string arguments = "")
    {
        await CallWithOutputAsync(outStub, outType, arguments);

        if (!FileTypes.Contains(outStub, outType, out FileType outActualType))
            throw new FileNotFoundException($"The output file set at \"{outStub}\" is missing the files [{outType & ~outActualType}] after calling PLINK with the additional arguments [{arguments.Trim()}]. We expected to find [{outType}] but only found [{outActualType}]. This is likely due to PLINK encountering an issue. See the associated log file.");
    }

    public async Task CallWithAndCheckAsync(string inStub, FileType inType, string outStub, FileType outType, string arguments = "")
    {
        await CallWithAsync(inStub, inType, outStub, outType, arguments);

        if (!FileTypes.Contains(outStub, outType, out FileType outActualType))
            throw new FileNotFoundException($"The output file set at \"{outStub}\" is missing the files [{outType & ~outActualType}] after calling PLINK with the additional arguments [{arguments.Trim()}]. We expected to find [{outType}] but only found [{outActualType}]. This is likely due to PLINK encountering an issue. See the associated log file.");
    }

    public async Task<FileType> CallWithAndCheckAsync(string inStub, string outStub, FileType outType, string arguments = "")
    {
        await CallWithAsync(inStub, out FileType inType, outStub, outType, arguments);

        if (!FileTypes.Contains(outStub, outType, out FileType outActualType))
            throw new FileNotFoundException($"The output file set at \"{outStub}\" is missing the files [{outType & ~outActualType}] after calling PLINK with the additional arguments [{arguments.Trim()}]. We expected to find [{outType}] but only found [{outActualType}]. This is likely due to PLINK encountering an issue. See the associated log file.");

        return inType;
    }
}
