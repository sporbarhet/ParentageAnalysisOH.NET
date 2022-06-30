using Microsoft.Extensions.Logging;
using Sporbarhet.Parentage.Analysis;
using Sporbarhet.Parentage.Extensions;
using Sporbarhet.Parentage.Plink;
using Sporbarhet.Parentage.Plink.Enums;

namespace Sporbarhet.Parentage;

public class ParentageAnalyzer
{
    protected ILogger? Logger { get; set; }

    public IPlinkService PlinkService { get; set; }

    public ParentageAnalyzer(IPlinkService plinkService, ILogger? logger = null)
    {
        Logger = logger;
        PlinkService = plinkService;
    }


    #region Parentage analysis
    protected OppositeHomozygoteAnalysis CountAndMatch(ZygosityDataset dataset, Int64 ohThreshold, bool doFullCount = false, string? parentAssignPath = null)
        => new OppositeHomozygoteAnalysis(dataset, Logger).CountAndMatchThreshold(ohThreshold, doFullCount, parentAssignPath);

    protected OppositeHomozygoteAnalysis CountAndMatch(ZygosityDataset dataset, double ohThresholdProportion, bool doFullCount = false, string? parentAssignPath = null)
    => new OppositeHomozygoteAnalysis(dataset, Logger).CountAndMatch(ohThresholdProportion, doFullCount, parentAssignPath);


    public OppositeHomozygoteAnalysis RunParentageAnalysis(string genedataStub, IDictionary<ID, Sex> parents, Int64 ohThreshold, bool doFullCount = false, string? parentAssignPath = null, IReadOnlyList<ID>? offspringIds = null)
        => CountAndMatch(new ZygosityDataset(genedataStub, parents, offspringIds, logger: Logger).LoadDataset(), ohThreshold, doFullCount, parentAssignPath);


    public OppositeHomozygoteAnalysis RunParentageAnalysis(string genedataStub, string parentsPath, Int64 ohThreshold, bool doFullCount = false, string? parentAssignPath = null, IReadOnlyList<ID>? offspringIds = null)
    => RunParentageAnalysis(genedataStub, PlinkFiles.ReadSexes(parentsPath), ohThreshold, doFullCount, parentAssignPath, offspringIds);

    public OppositeHomozygoteAnalysis RunParentageAnalysis(string genedataStub, IDictionary<ID, Sex> parents, double ohThresholdProportion, bool doFullCount = false, string? parentAssignPath = null, IReadOnlyList<ID>? offspringIds = null)
    => CountAndMatch(new ZygosityDataset(genedataStub, parents, offspringIds, logger: Logger).LoadDataset(), ohThresholdProportion, doFullCount, parentAssignPath);


    public OppositeHomozygoteAnalysis RunParentageAnalysis(string genedataStub, string parentsPath, double ohThresholdProportion, bool doFullCount = false, string? parentAssignPath = null, IReadOnlyList<ID>? offspringIds = null)
        => RunParentageAnalysis(genedataStub, PlinkFiles.ReadSexes(parentsPath), ohThresholdProportion, doFullCount, parentAssignPath, offspringIds);
    #endregion Parentage analysis


    #region Process one main file
    public async Task<OppositeHomozygoteAnalysis> ProcessOneMainFileAsync(int chromosomeSet, string genedataStub, string outStub, IDictionary<ID, Sex> parents, QualityControl qc, double hetStdDevs, Int64 ohThreshold, bool doFullCount = false, bool deleteIntermediateFiles = true, IReadOnlyList<ID>? offspringIds = null)
    {
        try
        {
            await PlinkService.ApplyQcHetAsync(chromosomeSet, genedataStub, outStub, FileType.Raw, qc, hetStdDevs, deleteIntermediateFiles);
            return RunParentageAnalysis(outStub, parents, ohThreshold, doFullCount, outStub + "_matches.csv", offspringIds);
        }
        finally
        {
            if (deleteIntermediateFiles)
                PlinkUtility.DeleteFilesExceptInfo(outStub);
        }
    }

    public async Task<OppositeHomozygoteAnalysis> ProcessOneMainFileAsync(int chromosomeSet, string genedataStub, string outStub, IDictionary<ID, Sex> parents, QualityControl qc, double hetStdDevs, double ohThresholdProportion, bool doFullCount = false, bool deleteIntermediateFiles = true, IReadOnlyList<ID>? offspringIds = null)
    {
        try
        {
            await PlinkService.ApplyQcHetAsync(chromosomeSet, genedataStub, outStub, FileType.Raw, qc, hetStdDevs, deleteIntermediateFiles);
            return RunParentageAnalysis(outStub, parents, ohThresholdProportion, doFullCount, outStub + "_matches.csv", offspringIds);
        }
        finally
        {
            if (deleteIntermediateFiles)
                PlinkUtility.DeleteFilesExceptInfo(outStub);
        }
    }

    public Task<OppositeHomozygoteAnalysis> ProcessOneMainFileAsync(int chromosomeSet, string genedataStub, string outStub, string inParentsPath, QualityControl qc, double hetStdDevs, Int64 ohThreshold, bool doFullCount = false, bool deleteIntermediateFiles = true, IReadOnlyList<ID>? offspringIds = null)
        => ProcessOneMainFileAsync(chromosomeSet, genedataStub, outStub, PlinkFiles.ReadSexes(inParentsPath), qc, hetStdDevs, ohThreshold, doFullCount, deleteIntermediateFiles, offspringIds);


    public Task<OppositeHomozygoteAnalysis> ProcessOneMainFileAsync(int chromosomeSet, string genedataStub, string outStub, string inParentsPath, QualityControl qc, double hetStdDevs, double ohThresholdProportion, bool doFullCount = false, bool deleteIntermediateFiles = true, IReadOnlyList<ID>? offspringIds = null)
        => ProcessOneMainFileAsync(chromosomeSet, genedataStub, outStub, PlinkFiles.ReadSexes(inParentsPath), qc, hetStdDevs, ohThresholdProportion, doFullCount, deleteIntermediateFiles, offspringIds);

    #endregion Process one main file


    #region Process with merge

    public async Task<OppositeHomozygoteAnalysis> ProcessWithMergeAsync(int chromosomeSet, string offspringStub, string parentsStub, string outStub, QualityControl qc, double hetStdDevs, Int64 ohThreshold, bool doFullCount = false, bool deleteIntermediateFiles = true, IReadOnlyList<ID>? offspringIds = null)
    {
        string mergeStub = outStub + "_merge";
        await MergeParentsAndOffspringAsync(chromosomeSet, offspringStub, parentsStub, mergeStub, deleteIntermediateFiles: deleteIntermediateFiles);
        try
        {
            return await ProcessOneMainFileAsync(chromosomeSet, mergeStub, outStub, parentsStub, qc, hetStdDevs, ohThreshold, doFullCount, deleteIntermediateFiles, offspringIds);
        }
        finally
        {
            if (deleteIntermediateFiles)
                PlinkUtility.DeleteFilesExceptInfo(mergeStub);
        }
    }

    public async Task<OppositeHomozygoteAnalysis> ProcessWithMergeAsync(int chromosomeSet, string offspringStub, string parentsStub, string outStub, QualityControl qc, double hetStdDevs, double ohThresholdProportion, bool doFullCount = false, bool deleteIntermediateFiles = true, IReadOnlyList<ID>? offspringIds = null)
    {
        string mergeStub = outStub + "_merge";
        await MergeParentsAndOffspringAsync(chromosomeSet, offspringStub, parentsStub, mergeStub, deleteIntermediateFiles: deleteIntermediateFiles);
        try
        {
            return await ProcessOneMainFileAsync(chromosomeSet, mergeStub, outStub, parentsStub, qc, hetStdDevs, ohThresholdProportion, doFullCount, deleteIntermediateFiles, offspringIds);
        }
        finally
        {
            if (deleteIntermediateFiles)
                PlinkUtility.DeleteFilesExceptInfo(mergeStub);
        }
    }


    public Task<OppositeHomozygoteAnalysis[]> ProcessAllWithMergeAsync(int chromosomeSet, IEnumerable<string> offspringStubs, IEnumerable<string> parentStubs, QualityControl qc, double hetStdDevs, Int64 ohThreshold, bool doFullCount = false, bool deleteIntermediateFiles = true, string outDirectory = "out", string outStubSuffix = " ~ OH", IReadOnlyList<ID>? offspringIds = null)
        => Task.WhenAll(offspringStubs.SelectMany(
                offspringStub => parentStubs.Select(
                    parentStub => ProcessWithMergeAsync(chromosomeSet, offspringStub, parentStub, Path.Join(outDirectory, @$"{PathExtensions.SplitFolderAndFilePart(offspringStub).FilePart} ~ {PathExtensions.SplitFolderAndFilePart(parentStub).FilePart}{outStubSuffix}"), qc, hetStdDevs, ohThreshold, doFullCount, deleteIntermediateFiles, offspringIds)
                )
            ));


    public Task<OppositeHomozygoteAnalysis[]> ProcessAllWithMergeAsync(int chromosomeSet, IEnumerable<string> offspringStubs, IEnumerable<string> parentStubs, QualityControl qc, double hetStdDevs, double ohThresholdProportion, bool doFullCount = false, bool deleteIntermediateFiles = true, string outDirectory = "out", string outStubSuffix = " ~ OH", IReadOnlyList<ID>? offspringIds = null)
    => Task.WhenAll(offspringStubs.SelectMany(
            offspringStub => parentStubs.Select(
                parentStub => ProcessWithMergeAsync(chromosomeSet, offspringStub, parentStub, Path.Join(outDirectory, @$"{PathExtensions.SplitFolderAndFilePart(offspringStub).FilePart} ~ {PathExtensions.SplitFolderAndFilePart(parentStub).FilePart}{outStubSuffix}"), qc, hetStdDevs, ohThresholdProportion, doFullCount, deleteIntermediateFiles, offspringIds)
            )
        ));


    protected async Task MergeParentsAndOffspringAsync(int chromosomeSet, string offspringStub, string parentsStub, string outStub, FileType outType = FileType.Binary, MergeMode mergeMode = MergeMode.Default, bool tryFlip = true, bool deleteIntermediateFiles = true)
    {
        // With PLINK2 the three function calls below could be simplified to a single --pmerge with the flag --variant-inner-join.
        // Sadly this flag is not yet (2.6.2022) fully implemented. It is, however, stated that it is a priority in their development,
        // so we can be hopeful...
        try
        {
            await Task.WhenAll( //TODO: this should be refactored out to PlinkService, as this is not needed when merging with PLINK 2.0
                PlinkService.ExtractAsync(chromosomeSet, offspringStub, parentsStub, outStub + "_offspring_intersect", FileType.Binary, deleteIntermediateFiles),
                PlinkService.ExtractAsync(chromosomeSet, parentsStub, offspringStub, outStub + "_parent_intersect", FileType.Binary, deleteIntermediateFiles)
            );
            await PlinkService.MergeAsync(chromosomeSet, outStub + "_offspring_intersect", outStub + "_parent_intersect", outStub, outType, mergeMode, tryFlip, deleteIntermediateFiles);
        }
        finally
        {
            if (deleteIntermediateFiles)
            {
                PlinkUtility.DeleteFilesExceptInfo(outStub + "_offspring_intersect");
                PlinkUtility.DeleteFilesExceptInfo(outStub + "_parent_intersect");
            }
        }
    }
    #endregion Process with merge


    public Task MergeParentGenedataAsync(int chromosomeSet, string inStub1, string inStub2, string outStub, FileType outType = FileType.Binary, MergeMode mergeMode = MergeMode.Default, bool tryFlip = true, bool convertIfPedMap = true, bool deleteIntermediateFiles = true)
    {
        string tmpInStub1 = inStub1, tmpInStub2 = inStub2;
        if (convertIfPedMap)
        {
            var type1 = FileTypes.GetFileTypes(inStub1);
            if (type1.Contains(FileType.PedMap) && !(type1.Contains(FileType.Binary) || type1.Contains(FileType.Pfile) || type1.Contains(FileType.Bpfile)))
                PlinkService.ConvertAsync(inStub1, tmpInStub1 = inStub1 + "_convert", FileType.Binary);

            var type2 = FileTypes.GetFileTypes(inStub2);
            if (type2.Contains(FileType.PedMap) && !(type2.Contains(FileType.Binary) || type2.Contains(FileType.Pfile) || type2.Contains(FileType.Bpfile)))
                PlinkService.ConvertAsync(inStub2, tmpInStub2 = inStub2 + "_convert", FileType.Binary);
        }

        try
        {
            return PlinkService.MergeAsync(chromosomeSet, tmpInStub1, tmpInStub2, outStub, outType, mergeMode, tryFlip, deleteIntermediateFiles);
        }
        finally
        {
            if (tmpInStub1 != inStub1)
                PlinkUtility.DeleteFilesExceptInfo(tmpInStub1);
            if (tmpInStub2 != inStub2)
                PlinkUtility.DeleteFilesExceptInfo(tmpInStub2);
        }
    }
}
