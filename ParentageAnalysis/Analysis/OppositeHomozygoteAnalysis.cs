using Microsoft.Extensions.Logging;
using Sporbarhet.Parentage.Extensions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Sporbarhet.Parentage.Analysis;

/// <summary>
/// An object describing an OH analysis.
/// </summary>
public class OppositeHomozygoteAnalysis : IDisposable
{
    protected ILogger? Logger;
    protected IDisposable? outerLogState;


    public ZygosityDataset Dataset;
    public /*OH*/object? OhThreshold;
    public bool FullCount = false;

    public /*OH[,]?*/object? OhCounts;
    public bool? OhCountsTransposed;

    public KinAssignment[]? ParentAssignments;

    [MemberNotNullWhen(true, nameof(OhCounts))]
    public bool IsCounted => OhCounts is not null;

    public OppositeHomozygoteAnalysis(ZygosityDataset dataset, ILogger? logger = null)
    {
        Dataset = dataset;
        Logger = logger;
        outerLogState = Logger?.BeginScope("Analysis of dataset \"{}\"", dataset.GenedataStub);
    }

    [MemberNotNull(nameof(OhCounts))]
    public OppositeHomozygoteAnalysis Count<OH>(OH ohThreshold, bool doFullCount = false)
        where OH : unmanaged
    {
        using var logScope = Logger?.BeginScope("Counting OH loci");

        if (!Dataset.IsLoaded)
            throw new InvalidOperationException("Cannot count opposite homozygote loci as the zygosity dataset is not loaded.");
        if (IsCounted)
            throw new InvalidOperationException("This analysis already contains opposite homozygoty counts. Consider creating a new analysis object.");

        if (Logger is not null)
        {
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("Performing {} count of opposite homozygote loci with a threshold of {}.", doFullCount ? "full" : "early terminated", ohThreshold/*, Dataset.OffspringIds.Count, Dataset.Parents.Count*/);

            if (doFullCount)
            {
                // Check OH type size issues
                ulong tOhMaxValue = (1UL << Marshal.SizeOf<OH>() * 8) - 1UL;
                ulong variantCount = (ulong)Dataset.VariantCount;
                if (tOhMaxValue < variantCount)
                    Logger?.LogWarning("The chosen type for opposite homozygote counts ({}) may not be large enough to hold the full counts. The maximum number of distinct values this type may represent is {}, while there are {} variants in the dataset. Consider using a larger number type.", typeof(OH), tOhMaxValue, variantCount);
            }
        }

        var sw = Stopwatch.StartNew();
        (OH[,] ohCounts, bool transposed) = OppositeHomozygote.Count(Dataset.Zygosities, Dataset.OffspringIds, Dataset.ParentIds, ohThreshold, doFullCount);
        OhCounts = ohCounts;
        OhCountsTransposed = transposed;
        OhThreshold = ohThreshold;
        FullCount = doFullCount;
        sw.Stop();

        if (Logger is not null && Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("{} opposite homozygote count took {} seconds.", doFullCount ? "Full" : "Early terminated", sw.ElapsedSeconds());

            Histogram<OH>? histAll = null;
            if (doFullCount)
            {
                histAll = new Histogram<OH>(ohCounts.Enumerate(), 24);
                Logger.LogDebug(@"OH loci histogram linear
{}
This plot shows the distribution of opposite homozygote counts across the entire dataset.
The ticks along the vertical axis denote the smallest value which is counted in that bar.", histAll.PlotString(64, AxisScale.Linear, xMinTickWidth: 5));
            }//The horizontal axis is scaled quadratically, as we expect the relationship between matches and non-matches to be quadratic.

            int ohThreshInt = Convert.ToInt32(ohThreshold);
            if (0 < ohThreshInt)
            {
                Histogram<OH> histThresh = new Histogram<OH>(ohCounts.Enumerate(), ohThreshInt, 0, ohThreshInt);
                if (histThresh.Buckets[0] + histThresh.Buckets[^1] < (ulong)ohCounts.LongLength) // Any values inside histogram
                    Logger.LogDebug(@"OH low count histogram
{}
This plot shows the distribution of all opposite homozygote counts below the threshold.
Note that here the horizontal axis is scaled linearly.
The ticks along the vertical axis denote the lowest value which is counted in that bar.", histThresh.PlotString(64, xMinTickWidth: 5, trimXaxis: true));
            }
        }

        return this;
    }


    [MemberNotNull(nameof(ParentAssignments))]
    protected OppositeHomozygoteAnalysis Match<OH>()
        where OH : unmanaged, IComparable<OH>
    {
        using var logScope = Logger?.BeginScope("Matching based on OH count");

        if (!Dataset.IsIdsLoaded)
            throw new InvalidOperationException("Dataset ids are not loaded.");
        if (!IsCounted)
            throw new InvalidOperationException("Opposing homozygote count have not been counted.");

        if (OhCounts is not OH[,] ohCounts)
            throw new ArgumentException(null, nameof(OH)); //TODO

        var sw = Stopwatch.StartNew();
        if ((bool)OhCountsTransposed!)
            ParentAssignments = KinAssignment.AssignKinTransposed(Dataset.OffspringIds, Dataset.Parents.Keys, (OH)OhThreshold!, ohCounts).ToArray(Dataset.OffspringIds.Count);
        else
            ParentAssignments = KinAssignment.AssignKin(Dataset.OffspringIds, Dataset.Parents.Keys, (OH)OhThreshold!, ohCounts).ToArray(Dataset.OffspringIds.Count);
        sw.Stop();

        if (Logger is not null && Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogDebug("Parent matching took {elapsed} seconds.", sw.ElapsedSeconds());

            int off0Match = ParentAssignments.Where(pa => pa.Kin.Count == 0).Count();
            int offNMatch = ParentAssignments.Where(pa => pa.Kin.Count > 1).Count();
            int off1Match = ParentAssignments.Length - off0Match - offNMatch;
            int parNMatch = ParentAssignments.SelectMany(pa => pa.Kin.Select(ap => ap.Id)).Distinct().Count();
            int par0Match = Dataset.Parents.Count - parNMatch;
            double offToPercent = 100d / Dataset.OffspringIds.Count;
            double parToPercent = 100d / Dataset.Parents.Count;

            Logger.LogInformation(
@"Found {} matches between {offspringCount} offspring and {parentCount} parents.
Parent assignment stats:
    Offspring with no matches: {off0Match} ({off0MatchPercent} %)
    Offspring with one match: {off1Match} ({off1MatchPercent} %)
    Offspring with multiple matches: {offNMatch} ({offNMatchPercent} %)

    Parents with no matches: {par0Match} ({par0MatchPercent} %)
    Parents with any matches: {parNMatch} ({parNMatchPercent} %)
",
                ParentAssignments.Select(pa => pa.Kin.Count).Sum(), Dataset.OffspringIds.Count, Dataset.Parents.Count,
                off0Match, Math.Round(off0Match * offToPercent, 2),
                off1Match, Math.Round(off1Match * offToPercent, 2),
                offNMatch, Math.Round(offNMatch * offToPercent, 2),
                par0Match, Math.Round(par0Match * parToPercent, 2),
                parNMatch, Math.Round(parNMatch * parToPercent, 2));

            HashSet<ID> sires = Dataset.Parents.Where(kv => kv.Value is 'M' or '1' or 'm').Select(kv => kv.Key).ToHashSet();
            if (sires.Count > 0)
            {
                int fatherNMatch = ParentAssignments.SelectMany(pa => pa.Kin.Select(ap => ap.Id)).Distinct().Count(id => sires.Contains(id));
                int father0Match = sires.Count - fatherNMatch;
                Logger.LogInformation(
                    "Out of {} sires in the dataset, {} matched with one or more offspring while {} matched with none.", sires.Count, fatherNMatch, father0Match
                );
            }

            HashSet<ID> dams = Dataset.Parents.Where(kv => kv.Value is 'F' or '2' or 'f').Select(kv => kv.Key).ToHashSet();
            if (dams.Count > 0)
            {
                int motherNMatch = ParentAssignments.SelectMany(pa => pa.Kin.Select(ap => ap.Id)).Distinct().Count(id => dams.Contains(id));
                int mother0Match = dams.Count - motherNMatch;
                Logger.LogInformation(
                    "Out of {} dams in the dataset, {} matched with one or more offspring while {} matched with none.", dams.Count, motherNMatch, mother0Match
                );
            }
        }

        return this;
    }

    protected OppositeHomozygoteAnalysis CountAndMatch<OH>(OH ohThreshold, bool doFullCount = false, string? parentAssignPath = null)
        where OH : unmanaged, IComparable<OH>
    {
        Logger?.LogInformation("Using a OH threshold of {} with a data type of {}.", ohThreshold, typeof(OH));

        if (Type.GetTypeCode(typeof(OH)) is TypeCode.Double or TypeCode.Single or TypeCode.Decimal)
            throw new ArgumentException(null, nameof(OH)); //TODO

        Count<OH>(ohThreshold, doFullCount);
        Match<OH>();
        if (parentAssignPath is not null)
        {
            if (ParentAssignments.Any(pa => pa.Kin.Count > 0))
                WriteMatches(parentAssignPath);
            else
                Logger?.LogDebug("There were no matches, so a results file will not be created.");
        }

        return this;
    }


    public OppositeHomozygoteAnalysis CountAndMatchThreshold(Int64 ohThreshold, bool doFullCount = false, string? parentAssignPath = null)
    {
        if (!Dataset.IsLoaded)
            throw new InvalidOperationException("Cannot count opposite homozygote loci as the zygosity dataset is not loaded.");
        if (ohThreshold < 1)
            throw new ArgumentOutOfRangeException(nameof(ohThreshold), "OH threshold is below 1. Did you mean to pass OH threshold proportion instead?");

        Int64 comparisonValue = doFullCount ? (Int64)Dataset.VariantCount : ohThreshold;

        return comparisonValue switch
        {
            <= byte.MaxValue => CountAndMatch((byte)ohThreshold, doFullCount, parentAssignPath),
            <= UInt16.MaxValue => CountAndMatch((UInt16)ohThreshold, doFullCount, parentAssignPath),
            <= UInt32.MaxValue => CountAndMatch((UInt32)ohThreshold, doFullCount, parentAssignPath),
            _ => CountAndMatch(ohThreshold, doFullCount, parentAssignPath),
        };
    }


    public OppositeHomozygoteAnalysis CountAndMatch(double ohThresholdProportion, bool doFullCount = false, string? parentAssignPath = null)
    {
        if (!Dataset.IsLoaded)
            throw new InvalidOperationException("Cannot count opposite homozygote loci as the zygosity dataset is not loaded.");
        if (ohThresholdProportion > 1)
            throw new ArgumentOutOfRangeException(nameof(ohThresholdProportion), "OH treshold proportion is above 1. Did you mean to pass OH threshold instead?");

        Int64 ohThreshold = (Int64)(ohThresholdProportion * (double)Dataset.VariantCount);
        return CountAndMatchThreshold(ohThreshold, doFullCount, parentAssignPath);
    }


    public void WriteMatches(string parentAssignPath)
    {
        if (ParentAssignments is null)
            throw new InvalidOperationException("Samples have not been matched.");
        if (Dataset.Parents is null)
            throw new InvalidOperationException("Parents not loaded in dataset.");

        KinAssignment.WriteAssignments(parentAssignPath, ParentAssignments, Dataset.Parents);
        Logger?.LogInformation("Parent assignments written to \"{}\".", parentAssignPath);
    }


    public void Dispose()
    {
        outerLogState?.Dispose();
        Dataset = null!;
        GC.SuppressFinalize(this);
    }
}
