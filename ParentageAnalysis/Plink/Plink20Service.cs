using Sporbarhet.Parentage.Plink.Enums;

namespace Sporbarhet.Parentage.Plink;

/// <summary>
/// A <see cref="IPlinkService"/> that makes use of PLINK version 2.
/// </summary>
internal class Plink20Service : IPlinkService
{
    public Task ApplyQcAsync(int chromosomeSet, string inStub, FileType inType, string outStub, FileType outType, QualityControl qc) => throw new NotImplementedException();
    public Task ApplyQcAsync(int chromosomeSet, string inStub, string outStub, FileType outType, QualityControl qc) => throw new NotImplementedException();
    public Task ApplyQcHetAsync(int chromosomeSet, string inStub, string outStub, FileType outType, QualityControl qc, double hetStdDevs, bool deleteIntermediateFiles = true) => throw new NotImplementedException();
    public Task ConvertAsync(string inStub, string outStub, FileType outType) => throw new NotImplementedException();
    public Task ExtractAsync(int chromosomeSet, string targetStub, string sourcePath, string outStub, FileType outType, bool deleteIntermediateFiles = true) => throw new NotImplementedException();
    public Task MergeAsync(int chromosomeSet, string targetStub, string sourceStub, string outStub, FileType outType = FileType.Binary, MergeMode mergeMode = MergeMode.Default, bool tryFlip = true, bool deleteIntermediateFiles = true) => throw new NotImplementedException();
}
