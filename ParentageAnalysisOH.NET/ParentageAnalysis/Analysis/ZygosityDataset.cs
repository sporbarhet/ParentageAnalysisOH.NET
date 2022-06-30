using Microsoft.Extensions.Logging;
using Sporbarhet.Parentage.BitCollections;
using Sporbarhet.Parentage.Extensions;
using Sporbarhet.Parentage.Plink;
using Sporbarhet.Parentage.Plink.Enums;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Sporbarhet.Parentage.Analysis;

/// <summary>
/// 
/// </summary>
/// To be used when we want to read the actual geno data into memory.
public class ZygosityDataset : IDisposable
{
    protected ILogger? Logger;
    protected IDisposable? outerLogState;

    ///// <summary>
    ///// Path to the parents file. If no file extension is given it will be inferred. Note that .csv files may not be inferred.
    ///// </summary>
    //public string ParentsPath;
    /// <summary>
    /// Path to the dataset files containing sample ids and information on their zygosities.
    /// Must include a .raw file. If a .fam or .ped file is also present, these will be used to read offspring ids.
    /// </summary>
    public string GenedataStub;
    public FileType? DatasetType;

    /// <summary>
    /// A <see cref="IDictionary{TKey, TValue}"/> of parents ids and sex.
    /// This is loaded from the files at <see cref="ParentsPath"/> with the <see cref="LoadIds"/> methods.
    /// </summary>
    public IDictionary<ID, Sex>? Parents { get; set; }

    protected IReadOnlyList<ID>? _parentIds;
    /// <summary>
    /// A <see cref="IReadOnlyList{T}"/> of parent ids.
    /// </summary>
    [NotNullIfNotNull(nameof(Parents))]
    [NotNullIfNotNull(nameof(_parentIds))]
    public IReadOnlyList<ID>? ParentIds => _parentIds ??= Parents?.Keys.ToArray(Parents.Count);

    /// <summary>
    /// A <see cref="IReadOnlyList{T}"/> of offsprings ids.
    /// This is loaded from the files at <see cref="GenedataStub"/> with the <see cref="LoadIds"/> methods.
    /// </summary>
    public IReadOnlyList<ID>? OffspringIds { get; protected set; }


    public IReadOnlyList<(string, char)>? Variants { get; protected set; }
    /// <summary>
    /// A <see cref="IDictionary{TKey, TValue}"/> of sample ids and their zygosity sequence.
    /// A zygosity sequence consists of the symbols '0', '1', '2', and typically 'N' (short for "NA").
    /// <list type="bullet">
    /// <item>'0' means that the sample is AA-homozygous at the current variant.</item>
    /// <item>'1' means that the sample is heterozygous (either AB or BA) at the current variant.</item>
    /// <item>'2' means that the sample is BB-homozygous at the current variant.</item>
    /// <item>Any other value means the zygosity is missing for the sample at that variant.</item>
    /// </list>
    /// See also <seealso cref="PlinkFiles.ReadRaw(string, char, int, int)"/>.
    /// </summary>
    // PERF: if the datasets become too large, it is possible to compress this dictionary in
    // memory by constructing a special datastructure where each variant only takes 2 bits of
    // memory, as we only need to express 4 possible values for each variant (0, 1, 2, or NA).
    // I.e. instead of having a char[] we would need some sort of TZyg[], packing 4 variants in a byte.
    public IDictionary<ID, (BitArrayL32 AA, BitArrayL32 BB)>? Zygosities { get; set; }

    public int? VariantCount => Variants?.Count;

    /// <summary>
    /// Asserts whether the ids are loaded, meaning that <see cref="Parents"/> and <see cref="OffspringIds"/> are initialized.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Parents), nameof(ParentIds), nameof(OffspringIds))]
    public bool IsIdsLoaded => Parents is not null && OffspringIds is not null; //TODO: this may be wrong now that we've changed to accepting parents as a parameter. Depends on what we consider "loaded".
    /// <summary>
    /// Asserts whether the dataset is fully loaded, meaning that the ids are loaded and that <see cref="Zygosities"/> is initialized.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Parents), nameof(ParentIds), nameof(OffspringIds), nameof(Zygosities), nameof(VariantCount))]
    public bool IsLoaded => IsIdsLoaded && Zygosities is not null;

    public ZygosityDataset(string genedataStub, IDictionary<ID, Sex> parents, IReadOnlyList<ID>? offspringIds = null, IDictionary<ID, (BitArrayL32 AA, BitArrayL32 BB)>? zygosities = null, ILogger? logger = null)
    {
        GenedataStub = genedataStub;
        Parents = parents;
        OffspringIds = offspringIds;
        Zygosities = zygosities;
        Logger = logger;
        outerLogState = Logger?.BeginScope("Dataset \"{}\"", GenedataStub);
    }


    private IEnumerable<ID> ReadSampleIds()
    {
        using var logScope = Logger?.BeginScope("Reading sample ids from \"{}\"", GenedataStub);
        var ids = PlinkFiles.ReadIds(GenedataStub, out FileType datasetType);
        DatasetType = datasetType;
        Logger?.LogDebug("Loaded {} sample ids from dataset at \"{}\" with file type {}.", ids.Count(), GenedataStub, datasetType);
        return ids;
    }

    [MemberNotNull(nameof(Parents))]
    private IDictionary<ID, Sex> CheckParents(IReadOnlySet<ID> datasetIds, out HashSet<ID> missingParents)
    {
        using var logScope = Logger?.BeginScope("Checking parent data");
        //if (Parents is null)
        //{
        //    Parents = ParentFiles.ReadParents(ParentsPath, out FileType parentFileType);
        //    Logger?.LogDebug("Loaded {} parents from \"{}\" with file type {}.", Parents.Count, ParentsPath, parentFileType);
        //}

        missingParents = new HashSet<ID>();
        foreach (ID id in Parents!.Keys)
            if (!datasetIds.Contains(id))
                missingParents.Add(id);

        foreach (ID id in missingParents)
            Parents.Remove(id);

        if (Logger is not null && Logger.IsEnabled(LogLevel.Warning) && missingParents.Count > 0)
        {
            Logger.LogWarning("There are {} parents missing from the dataset. These will be excluded from the analysis.", missingParents.Count);
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("The missing parents are:\r\n   {}", string.Join(',', missingParents));
        }

        return Parents;
    }


    [MemberNotNull(nameof(OffspringIds))]
    private IReadOnlyList<ID> CheckOffspringIds(IReadOnlySet<ID> datasetIds, IDictionary<ID, Sex> parents, out HashSet<ID>? missingOffspring, out HashSet<ID>? overlapIds)
    {
        using var logScope = Logger?.BeginScope("Loading offspring ids");

        if (OffspringIds is null)
        {
            // Assume all ids in dataset which are not parents are offspring.
            OffspringIds = datasetIds.Except(parents.Keys).ToArray(datasetIds.Count - parents.Count);
            overlapIds = null;
            missingOffspring = null;
        }
        else
        {
            // Check if parents and offspring overlap
            overlapIds = OffspringIds.ToHashSet();
            overlapIds.IntersectWith(parents.Keys);
            if (Logger is not null && Logger.IsEnabled(LogLevel.Warning) && overlapIds.Count > 0)
            {
                Logger.LogWarning("There are {} samples which are defined as both parent and offspring in this dataset.", overlapIds.Count);
                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("The samples defined as both parent and offspring are:\r\n   {}", string.Join(',', overlapIds));
            }

            // Check for missing offspring
            missingOffspring = new HashSet<ID>();
            foreach (ID id in OffspringIds)
                if (!datasetIds.Contains(id))
                    missingOffspring.Add(id);

            OffspringIds = OffspringIds.Except(missingOffspring).ToArray(OffspringIds.Count - missingOffspring.Count);

            if (Logger is not null && Logger.IsEnabled(LogLevel.Warning) && missingOffspring.Count > 0)
            {
                Logger.LogWarning("There are {} offspring missing from the dataset. These will be excluded from the analysis.", missingOffspring.Count);
                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("The missing offspring are:\r\n   {}", string.Join(',', missingOffspring));
            }
        }
        return OffspringIds;
    }


    [MemberNotNull(nameof(Parents), nameof(OffspringIds))]
    public ZygosityDataset LoadIds(IReadOnlySet<ID>? datasetIds = null) => LoadIds(out _, out _, out _, datasetIds);

    [MemberNotNull(nameof(Parents), nameof(OffspringIds))]
    public ZygosityDataset LoadIds(out HashSet<ID> missingParents, out HashSet<ID>? missingOffspring, out HashSet<ID>? overlapIds, IReadOnlySet<ID>? datasetIds = null)
    {
        using var logScope = Logger?.BeginScope("Loading ids");


        if (datasetIds == null)
            datasetIds = ReadSampleIds().ToHashSet();

        CheckParents(datasetIds, out missingParents);
        CheckOffspringIds(datasetIds, Parents, out missingOffspring, out overlapIds);
        return this;
    }

    [MemberNotNull(nameof(Parents), nameof(OffspringIds), nameof(Zygosities))]
    public ZygosityDataset LoadDataset()
    {
        using var logScope = Logger?.BeginScope("Loading dataset");
        string rawPath = Path.ChangeExtension(GenedataStub, "raw");

        var sw = Stopwatch.StartNew();
        try
        {
            (Zygosities, Variants) = (OffspringIds is not null && Parents is not null)
                ? PlinkFiles.ReadRawPair(rawPath, OffspringIds.Concat(Parents.Keys), retries: 6)
                : PlinkFiles.ReadRawPair(rawPath, retries: 6);
            //use dataset ids
            LoadIds(datasetIds: Zygosities.Keys.ToHashSet());
        }
        catch (FileNotFoundException)
        {
            Logger?.LogError("Could not load zygosity data from \"{}\" because the file was not found. Perhaps the path is wrong or PLINK threw an error while creating it. If the file is indeed there, consider increasing the timeout duration of this read operation.", rawPath);
            throw;
        }
        sw.Stop();
        if (Logger is not null)
        {
            Logger.LogDebug("Zygosity data loaded loaded from \"{}\" with {} samples and {} variants. The operation took {} seconds.", GenedataStub, Zygosities.Count, VariantCount, sw.ElapsedSeconds());

            if (Zygosities.Count < Parents.Count + OffspringIds.Count)
                Logger.LogWarning("There is expected to be at least {} samples in the zygosity data file, but only {} were found.", Parents.Count + OffspringIds.Count, Zygosities.Count);
        }
        return this;
    }

    public void UnloadDataset()
    {
        Zygosities = null;
        Logger?.LogTrace("Unloaded gene data.");
    }

    public void Dispose()
    {
        UnloadDataset();
        outerLogState?.Dispose();
        GC.SuppressFinalize(this);
    }
}
