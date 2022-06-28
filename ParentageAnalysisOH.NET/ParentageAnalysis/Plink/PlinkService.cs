using Microsoft.Data.Analysis;
using Microsoft.Extensions.Logging;
using Sporbarhet.Parentage.Analysis;
using Sporbarhet.Parentage.Extensions;
using Sporbarhet.Parentage.Plink.Enums;
using System.Diagnostics;
using System.Globalization;

namespace Sporbarhet.Parentage.Plink;

public class PlinkService : IPlinkService
{
    private ILogger? Logger { get; }

    private PlinkIO PlinkIO { get; }

    public PlinkService(PlinkIO plinkIO, ILogger? logger = null)
    {
        Logger = logger;
        PlinkIO = plinkIO;
    }

    /// <inheritdoc/>
    public async Task ApplyQcHetAsync(int chromosomeSet, string inStub, string outStub, FileType outType, QualityControl qc, double hetStdDevs, bool deleteIntermediateFiles = true)
    {
        using var logScope = Logger?.BeginScope("Quality control for \"{}\"", inStub);
        Stopwatch sw = Stopwatch.StartNew();

        if (hetStdDevs <= 0)
        {
            await ApplyQcAsync(chromosomeSet, inStub, outStub, outType, qc);
        }
        else
        {
            var inType = await PlinkIO.CallWithAndCheckAsync(inStub, outStub, FileType.Het, "".AddChromosomeSetArgument(chromosomeSet)); // Make het
            string hetFile = outStub + ".het";

            // Read heterozygosities from file
            var heterozygosities = PlinkFiles.ReadHet(hetFile, retries: 5);
            // Find heterozygosity bounds
            (double lower, double upper) = PlinkFiles.CalculateHeterozygosityBounds(heterozygosities, hetStdDevs);
            // Find samples with poor heterozygosity in dataset
            var hetPoor = heterozygosities[(PrimitiveDataFrameColumn<bool>)(heterozygosities["pHET"].ElementwiseLessThan(lower) | heterozygosities["pHET"].ElementwiseGreaterThan(upper))];

            if (Logger is not null && Logger.IsEnabled(LogLevel.Information))
            {
                string hetDesc = heterozygosities.Description()[Enumerable.Range(1, 3)].ToString().Indent(3);
                Logger.LogInformation(
    @"Heterozygosity file ""{hetPath}"" read ({hetRows} rows).
There are {hetPoorCount} samples with heterozygosity outside the bounds in this dataset.

Dataset statistics:
{hetDesc}
Heterozygosity bounds (mean ± {multipleStdDevsPoorHet} stddevs):
   Lower bound: {lower}
   Upper bound: {upper}
",
                hetFile, heterozygosities.Rows.Count, hetPoor.Rows.Count, hetDesc, hetStdDevs, string.Format("{0:0.00000}", lower), string.Format("{0:0.00000}", upper));

                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    var hist = new Histogram<double>((100d * (PrimitiveDataFrameColumn<double>)heterozygosities["pHET"]).OfType<double>(), 24);
                    Logger.LogDebug("Heterozygosities in percent\r\n{}\r\nThe vertical axis is heterozygosity in percent and the horizontal axis is the number of samples.", hist.PlotString(60, xDecimalPlaces: 1, xMinTickWidth: 5));
                }

                if (hetPoor.Rows.Count > 0 && Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("The samples with poor heterozygosity are:\r\n   {}", string.Join(',', (StringDataFrameColumn)hetPoor["IID"]));
            }

            string? poorFile = null;
            try
            {
                string arguments = "";
                if (hetPoor.Rows.Count > 0) // We have poor samples to filter out
                {
                    poorFile = Path.ChangeExtension(hetFile, "hetpoor"); // This extension is recognized in the FileType enum although it is not used by PLINK
                    DataFrame.WriteCsv(new DataFrame(hetPoor["FID"], hetPoor["IID"]), poorFile, ' ', false, null, CultureInfo.InvariantCulture);
                    arguments = $"--remove \"{poorFile}\"";
                }
                arguments = arguments.AddChromosomeSetArgument(chromosomeSet);
                await PlinkIO.CallWithAndCheckAsync(inStub, outStub, outType, $"{qc.GetPlinkCommand()} {arguments}");
            }
            finally
            {
                if (deleteIntermediateFiles)
                {
                    if (hetFile is not null)
                        File.Delete(hetFile);
                    if (poorFile is not null)
                        File.Delete(poorFile);
                }
            }
        }
        sw.Stop();
        Logger?.LogDebug("Applied quality control and placed the result at \"{}\". The operation took {} seconds.", outStub, sw.ElapsedSeconds());
    }

    /// <inheritdoc/>
    public Task ApplyQcAsync(int chromosomeSet, string inStub, string outStub, FileType outType, QualityControl qc)
        => PlinkIO.CallWithAndCheckAsync(inStub, outStub, outType, $"{qc.GetPlinkCommand()}".AddChromosomeSetArgument(chromosomeSet));

    /// <inheritdoc/>
    public Task ApplyQcAsync(int chromosomeSet, string inStub, FileType inType, string outStub, FileType outType, QualityControl qc)
        => PlinkIO.CallWithAndCheckAsync(inStub, inType, outStub, outType, $"{qc.GetPlinkCommand()}".AddChromosomeSetArgument(chromosomeSet));

    /// <inheritdoc/>
    public async Task ExtractAsync(int chromosomeSet, string targetStub, string sourcePath, string outStub, FileType outType, bool deleteIntermediateFiles = true)
    {
        using var logScope = Logger?.BeginScope("Extracting \"{}\" from \"{}\"", sourcePath, targetStub);

        //TODO: add support for .pvar files
        string sourceExtension = Path.GetExtension(sourcePath);
        if (string.IsNullOrEmpty(sourceExtension))
        {
            var sourceFileType = FileTypes.GetFileTypes(sourcePath);
            if (sourceFileType.HasFlag(FileType.Bim))
                sourcePath += sourceExtension = ".bim";
            else if (sourceFileType.HasFlag(FileType.Map))
                sourcePath += sourceExtension = ".map";
        }

        var df = sourceExtension switch
        {
            ".bim" => PlinkFiles.ReadBim(sourcePath),
            ".map" => PlinkFiles.ReadMap(sourcePath),
            _ => throw new FileNotFoundException(sourcePath)
        };

        DataFrame.WriteCsv(new DataFrame(df["VariantID"]), outStub + ".variants", header: false, cultureInfo: CultureInfo.InvariantCulture);

        try
        {
            await PlinkIO.CallWithAndCheckAsync(targetStub, outStub, outType, $"--extract \"{outStub}.variants\"".AddChromosomeSetArgument(chromosomeSet));
        }
        finally
        {
            if (deleteIntermediateFiles)
                File.Delete(outStub + ".variants");
        }
    }

    public async Task UpdateMapAsync(int chromosomeSet, string targetStub, string sourceMapPath, string outStub, FileType outType = FileType.Binary)
    {
        if (!Path.GetExtension(sourceMapPath).Equals(".map", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(null, nameof(sourceMapPath)); //TODO
        if (!File.Exists(sourceMapPath))
            throw new FileNotFoundException(nameof(sourceMapPath)); //TODO

        await PlinkIO.CallWithAndCheckAsync(
            targetStub,
            outStub,
            outType,
            $"--update-chr \"{sourceMapPath}\" 1 2 '#' --update-cm \"{sourceMapPath}\" 3 2 '#' --update-map \"{sourceMapPath}\" 4 2 '#'".AddChromosomeSetArgument(chromosomeSet)
        );
    }


    static readonly Dictionary<FileType, string> mergeFlags = new(4)
    {
        [FileType.Pfile] = "pmerge",
        [FileType.Binary] = "bmerge",
        [FileType.Bpfile] = "bpmerge",
        [FileType.PedMap] = "merge",
    };

    public async Task MergeAsync(int chromosomeSet, string targetStub, string sourceStub, string outStub, FileType outType = FileType.Binary, MergeMode mergeMode = MergeMode.Default, bool tryFlip = true, bool deleteIntermediateFiles = true)
    {
        using var logScope = Logger?.BeginScope("Merging \"{}\" and \"{}\"", targetStub, sourceStub);

        var sourceType = FileTypes.GetFileTypes(sourceStub);
        var targetType = FileTypes.GetFileTypes(targetStub);

        var mergeTypeKV = mergeFlags.Where(kv => sourceType.HasFlag(kv.Key)).FirstOrDefault();
        if (mergeTypeKV.Equals(default(KeyValuePair<FileType, string>)))
            throw new FileNotFoundException("PLINK genotype files not found. The specified files are not available.", sourceStub);

        await PlinkIO.CallWithAsync(targetStub, targetType, outStub, outType, $"--{mergeTypeKV.Value} \"{sourceStub}\" --merge-mode {(int)mergeMode}".AddChromosomeSetArgument(chromosomeSet));

        if (!File.Exists(outStub + "-merge.missnp"))
            return; //Success! //BUG: if input is not binary, this file will not be created even if the operation was not successful! //TODO: convert and try again (we can use a special merge mode in this case to hopefully speed up PLINK)

        string targetFlip = targetStub + "_flip";
        string sourceExclude = sourceStub + "_missnp_exclude";
        try
        {
            string newTarget = targetStub;
            var newTargetType = targetType;
            if (tryFlip)
            {
                Logger?.LogWarning("Conflicting markers were found. Attempting to flip the conflict markers in \"{}\" and write to \"{}\". See \"{}\" for a list of the conflict markers.", targetStub, targetFlip, outStub + "-flip.missnp");
                // Move to keep files, flip and try again
                File.Move(outStub + "-merge.missnp", outStub + "-flip.missnp");
                File.Move(outStub + "-merge.fam", outStub + "-flip.fam");
                await PlinkIO.CallWithAndCheckAsync(targetStub, targetType, targetFlip, FileType.Binary, $"--flip \"{outStub}-flip.missnp\"".AddChromosomeSetArgument(chromosomeSet));
                await PlinkIO.CallWithAsync(targetFlip, FileType.Binary, outStub, outType, $"--{mergeTypeKV.Value} \"{sourceStub}\" --merge-mode {(int)mergeMode}".AddChromosomeSetArgument(chromosomeSet));

                if (!File.Exists(outStub + "-merge.missnp"))
                    return; //Success!

                newTarget = targetFlip;
                newTargetType = FileType.Binary;
            }

            Logger?.LogWarning("Conflicting markers were found. Attempting to exclude the conflict markers. See \"{}\" for a list of the conflict markers.", outStub + "-merge.missnp");

            // Exclude and try again
            await PlinkIO.CallWithAndCheckAsync(sourceStub, sourceType, sourceExclude, FileType.Binary, $"--exclude \"{outStub}-merge.missnp\"".AddChromosomeSetArgument(chromosomeSet));
            await PlinkIO.CallWithAsync(newTarget, newTargetType, outStub, outType, $"--exclude \"{outStub}-merge.missnp\" --bmerge \"{sourceExclude}\" --merge-mode {(int)mergeMode}".AddChromosomeSetArgument(chromosomeSet));
        }
        finally
        {
            if (deleteIntermediateFiles)
            {
                if (tryFlip)
                    PlinkUtility.DeleteFilesExceptInfo(targetFlip);
                PlinkUtility.DeleteFilesExceptInfo(sourceExclude);
            }
        }
    }

    public async Task SetPhenoTypeAsync(int chromosomeSet, string inStub, string idFile, string outStub, FileType outType = FileType.Binary, Phenotype phenoType = Phenotype.DontSet, bool deleteIntermediateFiles = true)
    {
        if (phenoType == Phenotype.DontSet)
            return;

        // Read ids from original files
        var df = PlinkFiles.ReadDataFrame(idFile);
        // Write pheno files
        string phenoFile = inStub + ".pheno";
        DataFrame.WriteCsv(new DataFrame(df["FID"], df["IID"], DataFrameColumn.Create("Phenotype", Enumerable.Repeat((int)phenoType, (int)df.Rows.Count))), phenoFile, '\t', cultureInfo: CultureInfo.InvariantCulture);
        try
        {
            // Run Pheno command
            await PlinkIO.CallWithAndCheckAsync(inStub, outStub, outType, $"--pheno \"{phenoFile}\"".AddChromosomeSetArgument(chromosomeSet));
        }
        finally
        {
            if (deleteIntermediateFiles)
                File.Delete(phenoFile);
        }
    }

    /// <summary>
    /// --keep accepts a space/tab-delimited text file with family IDs in the first column and within-family IDs in the second column, and removes all unlisted samples from the current analysis. --remove does the same for all listed samples.
    /// <br/>
    /// See also <seealso href="https://www.cog-genomics.org/plink/1.9/filter"/>.
    /// </summary>
    /// <param name="targetStub"></param>
    /// <param name="ids"></param>
    /// <param name="outStub"></param>
    /// <param name="outType"></param>
    /// <param name="deleteIntermediateFiles"></param>
    /// <returns></returns>
    public async Task KeepAsync(int chromosomeSet, string targetStub, IEnumerable<(ID, ID)> ids, string outStub, FileType outType = FileType.Binary, bool deleteIntermediateFiles = true, string arguments = "")
    {
        string keepPath = outStub + "_keep_ids.csv";
        File.WriteAllLines(keepPath, ids.Select(id2 => id2.Item1 + " " + id2.Item2));

        try
        {
            await PlinkIO.CallWithAndCheckAsync(targetStub, outStub, outType, $"--keep \"{keepPath}\" {arguments}".AddChromosomeSetArgument(chromosomeSet));
        }
        finally
        {
            if (deleteIntermediateFiles)
                File.Delete(keepPath);
        }
    }
}
