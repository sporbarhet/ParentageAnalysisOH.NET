using Sporbarhet.Parentage.Plink.Enums;

namespace Sporbarhet.Parentage.Plink;

public interface IPlinkService
{
    /// <summary>
    /// Applies quality control (without heterozygosity considerations) and makes a file set of type <paramref name="outType"/>.
    /// See <see cref="QualityControl"/> for quality control options.
    /// </summary>
    /// <param name="chromosomeSet">The chromosome set that is passed to PLINK.</param>
    /// <param name="inStub">The input file stub. A stub is a path without a file extension.</param>
    /// <param name="inStub">The input file set type.</param>
    /// <param name="outStub">The output file stub. A stub is a path without a file extension. A binary PLINK file set and a raw file is placed at that location.</param>
    /// <param name="qc">Quality control parameters that are passed to PLINK as flags.</param>
    Task ApplyQcAsync(int chromosomeSet, string inStub, FileType inType, string outStub, FileType outType, QualityControl qc);


    /// <summary>
    /// Applies quality control (without heterozygosity considerations) and makes a file set of type <paramref name="outType"/>.
    /// See <see cref="QualityControl"/> for quality control options.
    /// </summary>
    /// <param name="chromosomeSet">The chromosome set that is passed to PLINK.</param>
    /// <param name="inStub">The input file stub. A stub is a path without a file extension.</param>
    /// <param name="outStub">The output file stub. A stub is a path without a file extension. A binary PLINK file set and a raw file is placed at that location.</param>
    /// <param name="qc">Quality control parameters that are passed to PLINK as flags.</param>
    Task ApplyQcAsync(int chromosomeSet, string inStub, string outStub, FileType outType, QualityControl qc);


    /// <summary>
    /// Applies quality control (PLINK and heterozygosity check) and makes a file set of type <paramref name="outType"/>.
    /// See <see cref="QualityControl"/> for quality control options.
    /// </summary>
    /// <param name="chromosomeSet">The chromosome set that is passed to PLINK.</param>
    /// <param name="inStub">The input file stub. A stub is a path without a file extension.</param>
    /// <param name="outStub">The output file stub. A stub is a path without a file extension. A binary PLINK file set and a raw file is placed at that location.</param>
    /// <param name="qc">Quality control parameterhs that are passed to PLINK as flags.</param>
    /// <param name="hetStdDevs">The number of standard deviations from the mean where heterozygosity is considered poor. Set to 0 or lower to disable.</param>
    /// <param name="deleteIntermediateFiles">Whether any intermediate files created by this method should be deleted before returning. Defaults to <see langword="true"/>.
    /// <br/>
    /// The intermediate files are a het file, a recode allele file and a file of samples with poor heterozygosity if any.</param>
    Task ApplyQcHetAsync(int chromosomeSet, string inStub, string outStub, FileType outType, QualityControl qc, double hetStdDevs, bool deleteIntermediateFiles = true);
    
    Task ExtractAsync(int chromosomeSet, string targetStub, string sourcePath, string outStub, FileType outType, bool deleteIntermediateFiles = true);

    Task MergeAsync(int chromosomeSet, string targetStub, string sourceStub, string outStub, FileType outType = FileType.Binary, MergeMode mergeMode = MergeMode.Default, bool tryFlip = true, bool deleteIntermediateFiles = true);

    Task ConvertAsync(string inStub, string outStub, FileType outType);
}
