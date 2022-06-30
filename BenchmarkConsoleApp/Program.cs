using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Data.Analysis;
using Sporbarhet.Parentage.Analysis;
using Sporbarhet.Parentage.BitCollections;
using Sporbarhet.Parentage.Extensions;

namespace Sporbarhet.Parentage.BenchmarkConsoleApp;

[SimpleJob(launchCount: 1, warmupCount: 4, targetCount: 20)]
[MemoryDiagnoser]
public class OppositeHomozygoteBenchmarks
{
    private readonly ZygosityDataset? ComparisonDataset;

    public OppositeHomozygoteBenchmarks()
    {
        DataFrame progeny = DataFrame.LoadCsv(@"Datasets\Pedigree.dat", ' ', dataTypes: new[] {typeof(string), typeof(string), typeof(string), typeof(char), typeof(int)} );
        Dictionary<string, char> parentsGen0 = progeny.Rows.Select(row => ((string)row[0], (char)row[3])).Where(pair => int.Parse(pair.Item1) < 301).ToDictionary(pair => pair.Item1, pair => pair.Item2);
        Dictionary<string, char> parentsGen1 = progeny.Rows.Select(row => ((string)row[0], (char)row[3])).Where(pair => int.Parse(pair.Item1) is > 300 and < 50301).ToDictionary(pair => pair.Item1, pair => pair.Item2);
        string[] generation1Ids = parentsGen1.Keys.ToArray();
        string[] generation2Ids = progeny.Rows.Select(row => (string)row[0]).Where(pair => int.Parse(pair) > 50300).ToArray();

        //ComparisonDataset = new ZygosityDataset(@"Datasets\300+30000 samples all markers.raw", parentsGen0, generation1Ids).LoadDataset();
        //ComparisonDataset = new ZygosityDataset(@"Datasets\3000+3000 samples all markers.raw", parentsGen1, generation2Ids).LoadDataset();
        //ComparisonDataset = new ZygosityDataset(@"Datasets\3000+3000 samples 15000 markers.raw", parentsGen1, generation2Ids).LoadDataset();
        //ComparisonDataset = new ZygosityDataset(@"Datasets\1000+5000 samples 15000 markers.raw", parentsGen1, generation2Ids).LoadDataset();
        ComparisonDataset = new ZygosityDataset(@"Datasets\1000+1000 samples 15000 markers.raw", parentsGen1, generation2Ids).LoadDataset();
    }

    [Benchmark]
    public void ComparisonGroupedShortCircuitByte() => ComputeCountsSingleThread(ComparisonDataset!.OffspringIds!.Select(id => ComparisonDataset!.Zygosities![id]).ToArray(), ComparisonDataset!.ParentIds!.Select(id => ComparisonDataset!.Zygosities![id]).ToArray(), (byte)((double)ComparisonDataset.VariantCount! * 0.0035), Convert.ToByte);
    
    [Benchmark]
    public void ComparisonGroupedCompleteUInt16() => ComputeCountsSingleThread(ComparisonDataset!.OffspringIds!.Select(id => ComparisonDataset!.Zygosities![id]).ToArray(), ComparisonDataset!.ParentIds!.Select(id => ComparisonDataset!.Zygosities![id]).ToArray(), UInt16.MaxValue, Convert.ToUInt16);
    
    [Benchmark]
    public void ComparisonUngroupedShortCircuitByte()
    {
        var zygosities = ComparisonDataset!.OffspringIds!.Concat(ComparisonDataset!.ParentIds!).Select(id => ComparisonDataset!.Zygosities![id]).ToArray();
        ComputeCountsAllSingleThread(zygosities, (byte)((double)ComparisonDataset.VariantCount! * 0.0035), Convert.ToByte);
    }
    
    [Benchmark]
    public void ComparisonUngroupedCompleteUInt16()
    {
        var zygosities = ComparisonDataset!.OffspringIds!.Concat(ComparisonDataset!.ParentIds!).Select(id => ComparisonDataset!.Zygosities![id]).ToArray();
        ComputeCountsAllSingleThread(zygosities, UInt16.MaxValue, Convert.ToUInt16);
    }

    /// <summary>
    /// Computes the opposing homozygous loci counts (OH counts), up to a max of <paramref name="threshold"/>, between zygosities in group 1 and zygosities in group 2 and returns the counts as a two dimensional array.
    /// The value at the (i,j)'th position in the output array gives the OH count between the i'th zygosity array in group 1 and the j'th zygosity array in group 2.
    /// <br/>
    /// See <seealso cref="VectorOperations.IntersectionCount2(ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, int)"/> for how the opposing homozygous loci count is computed.
    /// </summary>
    /// <remarks>
    /// The algorithm performs faster if the largest of the zygosity arrays is passed as <paramref name="zygosities1"/>.
    /// </remarks>
    /// <typeparam name="OH">
    /// The OH count type. The allowed values are <see cref="byte"/>, <see cref="UInt16"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Int16"/>, <see cref="Int32"/>, and <see cref="Int64"/>.
    /// A smaller data type is preferred due to better memory efficiency, but it must also be large enough to hold all possible OH counts for the given dataset.
    /// </typeparam>
    /// <param name="zygosities1">The zygosities of samples from group 1. For instance, group 1 can be the list of posed parents, and group 2 the list of posed offspring.</param>
    /// <param name="zygosities2">The zygosities of sample from group 2. For instance, group 1 can be the list of posed parents, and group 2 the list of posed offspring.</param>
    /// <param name="threshold">The OH count threshold. If the OH count exceeds this value for a given pair of samples, the computation is terminated early for that pair.</param>
    /// <param name="convert">The function to convert from <see cref="int"/> counts to <typeparamref name="OH"/>.</param>
    /// <returns>The opposing homozygous loci counts. The value at the (i,j)'th position gives the OH count between the zygosity of the i'th sample from group 1 and the zygosity of the j'th sample from group 2.</returns>
    static OH[,] ComputeCountsSingleThread<OH>(IReadOnlyList<(BitArrayL32 AA, BitArrayL32 BB)> zygosities1, IReadOnlyList<(BitArrayL32 AA, BitArrayL32 BB)> zygosities2, OH threshold, Func<int, OH> convert)
        where OH : unmanaged
    {
        int markerCount = zygosities1[0].AA.Count;
        int thresholdInt = Convert.ToInt32(threshold);

        // Whether we should ignore the threshold and just do a full count or not
        bool ignoreThreshold = thresholdInt * 32 > markerCount; //Very approximate

        OH[,] counts = new OH[zygosities1.Count, zygosities2.Count];


        var range = (0, Item2: zygosities1.Count);
        //Parallel.ForEach(Partitioner.Create(0, zygosities1.Count), (range, _) =>
        //{
        //    // Iterate over given range of group 1 samples
        for (int i1 = range.Item1; i1 < range.Item2; i1++)
        {
            (var AA1, var BB1) = zygosities1[i1];
            // Iterate over all samples in group 2
            for (int i2 = 0; i2 < zygosities2.Count; i2++)
            {
                (var AA2, var BB2) = zygosities2[i2];
                int sum = ignoreThreshold ?
                    VectorOperations.IntersectionCount2(AA1.Words, BB2.Words, AA2.Words, BB1.Words) :
                    VectorOperations.IntersectionCount2(AA1.Words, BB2.Words, AA2.Words, BB1.Words, thresholdInt); // short-circuit

                counts[i1, i2] = convert(Math.Min(sum, thresholdInt));
            }
        }
        //});
        return counts;
    }

    /// <summary>
    /// Computes the opposing homozygous loci counts (OH counts), up to a max of <paramref name="threshold"/>, between all zygosities and returns the counts as a two dimensional array.
    /// The value at the (i,j)'th position in the output array gives the OH count between the i'th zygosity array and the j'th zygosity array.
    /// <br/>
    /// See <seealso cref="VectorOperations.IntersectionCount2(ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, int)"/> for how the opposing homozygous loci count is computed.
    /// </summary>
    /// <remarks>
    /// The algorithm performs faster if the largest of the zygosity arrays is passed as <paramref name="zygosities1"/>.
    /// </remarks>
    /// <typeparam name="OH">
    /// The OH count type. The allowed values are <see cref="byte"/>, <see cref="UInt16"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Int16"/>, <see cref="Int32"/>, and <see cref="Int64"/>.
    /// A smaller data type is preferred due to better memory efficiency, but it must also be large enough to hold all possible OH counts for the given dataset.
    /// </typeparam>
    /// <param name="zygosities">The zygosities of samples.</param>
    /// <param name="threshold">The OH count threshold. If the OH count exceeds this value for a given pair of samples, the computation is terminated early for that pair.</param>
    /// <param name="convert">The function to convert from <see cref="int"/> counts to <typeparamref name="OH"/>.</param>
    /// <returns>The opposing homozygous loci counts. The value at the (i,j)'th position gives the OH count between the i'th sample and the j'th sample.</returns>
    static OH[,] ComputeCountsAllSingleThread<OH>(IReadOnlyList<(BitArrayL32 AA, BitArrayL32 BB)> zygosities, OH threshold, Func<int, OH> convert)
        where OH : unmanaged
    {
        int markerCount = zygosities[0].AA.Count;
        int thresholdInt = Convert.ToInt32(threshold);

        /// Whether we should ignore the threshold and just do a full count or not
        bool ignoreThreshold = thresholdInt * 32 > markerCount; //Very approximate

        OH[,] counts = new OH[zygosities.Count, zygosities.Count];


        var range = (0, Item2: zygosities.Count - 1);
        //Parallel.ForEach(Partitioner.Create(0, zygosities.Count - 1), (range, _) =>
        //{
        //    // Iterate over given range of samples
        for (int i1 = range.Item1; i1 < range.Item2; i1++)
        {
            (var AA1, var BB1) = zygosities[i1];
            // Iterate over samples after i1
            for (int i2 = i1 + 1; i2 < zygosities.Count; i2++)
            {
                (var AA2, var BB2) = zygosities[i2];
                int sum = ignoreThreshold ?
                    VectorOperations.IntersectionCount2(AA1.Words, BB2.Words, AA2.Words, BB1.Words) :
                    VectorOperations.IntersectionCount2(AA1.Words, BB2.Words, AA2.Words, BB1.Words, thresholdInt); // short-circuit

                counts[i2, i1] = counts[i1, i2] = convert(Math.Min(sum, thresholdInt));
            }
        }
        //});
        return counts;
    }

}

public class Program
{
    public static void Main() => _ = BenchmarkRunner.Run(typeof(Program).Assembly);
}
