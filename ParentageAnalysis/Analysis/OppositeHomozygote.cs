using Sporbarhet.Parentage.BitCollections;
using Sporbarhet.Parentage.Extensions;
using Sporbarhet.Parentage.Plink.Enums;
using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Sporbarhet.Parentage.Analysis;

/// <summary>
/// Methods for computing the opposite homozygote counts between samples
/// </summary>
public static class OppositeHomozygote
{
    /// <summary>
    /// Counts the opposing homozygous loci (OH count), up to a max of <paramref name="threshold"/>, between samples in group 1 and samples in group 2 and returns the counts as a two dimensional array.
    /// The value at the (i,j)'th position in the output array gives the OH count between the i'th sample in group 1 and the j'th sample in group 2.
    /// <br/>
    /// See <seealso cref="VectorOperations.IntersectionCount2(ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, int)"/> for how the opposing homozygous loci count is computed.
    /// </summary>
    /// <remarks>
    /// Even if <paramref name="doFullCount"/> is set, note that <typeparamref name="OH"/> is used to store the OH counts.
    /// Hence if this data type is not large to store the true counts, the return value will be clamped to the maximal value that that data type can hold.
    /// </remarks>
    /// <typeparam name="ID">The type of sample ids.</typeparam>
    /// <typeparam name="OH">
    /// The OH count type. The allowed values are <see cref="byte"/>, <see cref="UInt16"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Int16"/>, <see cref="Int32"/>, and <see cref="Int64"/>.
    /// A smaller data type is preferred due to better memory efficiency, but it must also be large enough to hold all possible OH counts for the given dataset.
    /// <br/>
    /// <example>
    /// If <paramref name="doFullCount"/> is <see cref="false"/>, then a good choice for <typeparamref name="OH"/> is the smallest unsigned integer type which can represent the value of <paramref name="threshold"/>.
    /// For instance, if <paramref name="threshold"/> is <c>255</c>, then the best choice for <typeparamref name="OH"/> is <see cref="byte"/>.
    /// </example>
    /// </typeparam>
    /// <param name="zygosities">The dictionary to read zygosities from.</param>
    /// <param name="group1">The sample ids of group 1. For instance, group 1 can be the list of posed parents, and group 2 the list of posed offspring.</param>
    /// <param name="group2">The sample ids of group 2. For instance, group 1 can be the list of posed parents, and group 2 the list of posed offspring.</param>
    /// <param name="threshold">The OH count threshold. If the OH count exceeds this value for a given pair of samples, the computation is terminated early for that pair. If <paramref name="doFullCount"/> is set, then this parameter is ignored.</param>
    /// <param name="doFullCount">Whether to perform a full count, in effect ignoring the OH threshold, or not.</param>
    /// <returns>The opposing homozygous loci counts. The value at the (i,j)'th position gives the OH count between the i'th sample of group 1 and the j'th sample of group 2.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="zygosities"/>, <paramref name="group1"/> or <paramref name="group2"/> is empty.</exception>
    public static (OH[,] Counts, bool Transposed) Count<ID, OH>(IDictionary<ID, (BitArrayL32 AA, BitArrayL32 BB)> zygosities, IReadOnlyList<ID> group1, IReadOnlyList<ID> group2, OH threshold, bool doFullCount = false)
        where OH : unmanaged
    {
        if (zygosities.Count == 0)
            throw new ArgumentException("Zygosity dictionary is empty.", nameof(zygosities));
        if (group1.Count == 0)
            throw new ArgumentException("Group 1 is empty.", nameof(group1));
        if (group2.Count == 0)
            throw new ArgumentException("Group 2 is empty.", nameof(group2));

        var group1Zygosities = group1.Select(id => zygosities[id]).ToArray(group1.Count);
        var group2Zygosities = group2.Select(id => zygosities[id]).ToArray(group2.Count);

        return Count(group1Zygosities, group2Zygosities, threshold, doFullCount);
    }


    /// <summary>
    /// Counts the opposing homozygous loci (OH count), up to a max of <paramref name="threshold"/>, between zygosities in group 1 and zygosities in group 2 and returns the counts as a two dimensional array.
    /// The value at the (i,j)'th position in the output array gives the OH count between the i'th zygosity array in group 1 and the j'th zygosity array in group 2.
    /// <br/>
    /// See <seealso cref="VectorOperations.IntersectionCount2(ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, int)"/> for how the opposing homozygous loci count is computed.
    /// </summary>
    /// <remarks>
    /// Even if <paramref name="doFullCount"/> is set, note that <typeparamref name="OH"/> is used to store the OH counts.
    /// Hence if this data type is not large to store the true counts, the return value will be clamped to the maximal value that that data type can hold.
    /// </remarks>
    /// <typeparam name="OH">
    /// The OH count type. The allowed values are <see cref="byte"/>, <see cref="UInt16"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Int16"/>, <see cref="Int32"/>, and <see cref="Int64"/>.
    /// A smaller data type is preferred due to better memory efficiency, but it must also be large enough to hold all possible OH counts for the given dataset.
    /// <br/>
    /// <example>
    /// If <paramref name="doFullCount"/> is <see cref="false"/>, then a good choice for <typeparamref name="OH"/> is the smallest unsigned integer type which can represent the value of <paramref name="threshold"/>.
    /// For instance, if <paramref name="threshold"/> is <c>255</c>, then the best choice for <typeparamref name="OH"/> is <see cref="byte"/>.
    /// </example>
    /// </typeparam>
    /// <param name="zygosities1">The zygosities of samples from group 1. For instance, group 1 can be the list of posed parents, and group 2 the list of posed offspring.</param>
    /// <param name="zygosities2">The zygosities of sample from group 2. For instance, group 1 can be the list of posed parents, and group 2 the list of posed offspring.</param>
    /// <param name="threshold">The OH count threshold. If the OH count exceeds this value for a given pair of samples, the computation is terminated early for that pair. If <paramref name="doFullCount"/> is set, then this parameter is ignored.</param>
    /// <param name="doFullCount">Whether to perform a full count, in effect ignoring the OH threshold, or not.</param>
    /// <returns>The opposing homozygous loci counts. The value at the (i,j)'th position gives the OH count between the zygosity of the i'th sample from group 1 and the zygosity of the j'th sample from group 2.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="zygosities1"/>, or <paramref name="zygosities2"/> is empty.</exception>
    public static (OH[,] Counts, bool Transposed) Count<OH>(IReadOnlyList<(BitArrayL32 AA, BitArrayL32 BB)> zygosities1, IReadOnlyList<(BitArrayL32 AA, BitArrayL32 BB)> zygosities2, OH threshold, bool doFullCount = false)
        where OH : unmanaged
    {
        if (zygosities1.Count == 0)
            throw new ArgumentException("Group 1 zygosities is empty.", nameof(zygosities1));
        if (zygosities2.Count == 0)
            throw new ArgumentException("Group 2 zygosities is empty.", nameof(zygosities2));

        // Count opposite homozygotes based on OH type
        return ((OH[,], bool))(threshold switch
        {
            byte thresh => ((object, bool))ComputeCounts(zygosities1, zygosities2, doFullCount ? byte.MaxValue : thresh, Convert.ToByte),
            UInt16 thresh => ComputeCounts(zygosities1, zygosities2, doFullCount ? UInt16.MaxValue : thresh, Convert.ToUInt16),
            UInt32 thresh => ComputeCounts(zygosities1, zygosities2, doFullCount ? UInt32.MaxValue : thresh, Convert.ToUInt32),
            UInt64 thresh => ComputeCounts(zygosities1, zygosities2, doFullCount ? UInt64.MaxValue : thresh, Convert.ToUInt64),
            Int16 thresh => ComputeCounts(zygosities1, zygosities2, doFullCount ? Int16.MaxValue : thresh, Convert.ToInt16),
            Int32 thresh => ComputeCounts(zygosities1, zygosities2, doFullCount ? Int32.MaxValue : thresh, Convert.ToInt32),
            Int64 thresh => ComputeCounts(zygosities1, zygosities2, doFullCount ? Int64.MaxValue : thresh, Convert.ToInt64),
            _ => throw new ArgumentException($"The type parameter {nameof(OH)} must be one of the types {typeof(byte)}, {typeof(UInt16)},{typeof(UInt32)},{typeof(UInt64)},{typeof(Int16)},{typeof(Int32)},or {typeof(Int64)}, but the type {typeof(OH)} was given.", nameof(OH)),
        });
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
    static (OH[,] Counts, bool Transposed) ComputeCounts<OH>(IReadOnlyList<(BitArrayL32 AA, BitArrayL32 BB)> zygosities1, IReadOnlyList<(BitArrayL32 AA, BitArrayL32 BB)> zygosities2, OH threshold, Func<int, OH> convert)
        where OH : unmanaged
    {
        if (zygosities1.Count < zygosities2.Count)
            return (ComputeCounts(zygosities2, zygosities1, threshold, convert).Counts, true);

        int markerCount = zygosities1[0].AA.Count;
        int thresholdInt = Convert.ToInt32(threshold);

        /// Whether we should ignore the threshold and just do a full count or not
        bool ignoreThreshold = thresholdInt * 32 > markerCount; //Very approximate

        OH[,] counts = new OH[zygosities1.Count, zygosities2.Count];

        Parallel.ForEach(Partitioner.Create(0, zygosities1.Count), (range, _) =>
        {
            // Iterate over given range of group 1 samples
            for (int i1 = range.Item1; i1 < range.Item2; i1++)
            {
                (var AA1, var BB1) = zygosities1[i1];
                // Iterate over all samples in group 2
                for (int i2 = 0; i2 < zygosities2.Count; i2++)
                {
                    (var AA2, var BB2) = zygosities2[i2];
                    int sum = ignoreThreshold ?
                        VectorOperations.IntersectionCount2(AA1.Words, BB2.Words, AA2.Words, BB1.Words) :
                        VectorOperations.IntersectionCount2(AA1.Words, BB2.Words, AA2.Words, BB1.Words, thresholdInt); // early termination

                    counts[i1, i2] = convert(Math.Min(sum, thresholdInt));
                }
            }
        });

        return (counts, false);
    }


    /// <inheritdoc cref="CountAll{ID, OH}(IReadOnlyList{(BitArrayL32 AA, BitArrayL32 BB)}, OH, bool)"/>
    public static OH[,] CountAll<ID, OH>(IDictionary<ID, (BitArrayL32 AA, BitArrayL32 BB)> zygosities, OH threshold, bool doFullCount = false)
        where OH : unmanaged => CountAll<ID, OH>(zygosities.Values.ToArray(), threshold, doFullCount);


    /// <summary>
    /// Counts the opposing homozygous loci (OH count), up to a max of <paramref name="threshold"/>, between all samples and returns the counts as a two dimensional array.
    /// The value at the (i,j)'th position in the output array gives the OH count between the i'th sample and the j'th sample.
    /// <br/>
    /// See <seealso cref="VectorOperations.IntersectionCount2(ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, ReadOnlySpan{ulong}, int)"/> for how the opposing homozygous loci count is computed.
    /// </summary>
    /// <remarks>
    /// Even if <paramref name="doFullCount"/> is set, note that <typeparamref name="OH"/> is used to store the OH counts.
    /// Hence if this data type is not large to store the true counts, the return value will be clamped to the maximal value that that data type can hold.
    /// </remarks>
    /// <typeparam name="ID">The type of sample ids.</typeparam>
    /// <typeparam name="OH">
    /// The OH count type. The allowed values are <see cref="byte"/>, <see cref="UInt16"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Int16"/>, <see cref="Int32"/>, and <see cref="Int64"/>.
    /// A smaller data type is preferred due to better memory efficiency, but it must also be large enough to hold all possible OH counts for the given dataset.
    /// <br/>
    /// <example>
    /// If <paramref name="doFullCount"/> is <see cref="false"/>, then a good choice for <typeparamref name="OH"/> is the smallest unsigned integer type which can represent the value of <paramref name="threshold"/>.
    /// For instance, if <paramref name="threshold"/> is <c>255</c>, then the best choice for <typeparamref name="OH"/> is <see cref="byte"/>.
    /// </example>
    /// </typeparam>
    /// <param name="zygosities">The collection of zygosities.</param>
    /// <param name="threshold">The OH count threshold. If the OH count exceeds this value for a given pair of samples, the computation is terminated early for that pair. If <paramref name="doFullCount"/> is set, then this parameter is ignored.</param>
    /// <param name="doFullCount">Whether to perform a full count, in effect ignoring the OH threshold, or not.</param>
    /// <returns>The opposing homozygous loci counts. The value at the (i,j)'th position gives the OH count between the i'th sample and the j'th sample.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="zygosities"/> collection is empty.</exception>
    public static OH[,] CountAll<ID, OH>(IReadOnlyList<(BitArrayL32 AA, BitArrayL32 BB)> zygosities, OH threshold, bool doFullCount = false)
        where OH : unmanaged
    {
        if (zygosities.Count == 0)
            throw new ArgumentException("Zygosities is empty.", nameof(zygosities));

        // Count opposite homozygotes based on OH type
        return (OH[,])(threshold switch
        {
            byte thresh => (object)ComputeCountsAll(zygosities, doFullCount ? byte.MaxValue : thresh, Convert.ToByte),
            UInt16 thresh => ComputeCountsAll(zygosities, doFullCount ? UInt16.MaxValue : thresh, Convert.ToUInt16),
            UInt32 thresh => ComputeCountsAll(zygosities, doFullCount ? UInt32.MaxValue : thresh, Convert.ToUInt32),
            UInt64 thresh => ComputeCountsAll(zygosities, doFullCount ? UInt64.MaxValue : thresh, Convert.ToUInt64),
            Int16 thresh => ComputeCountsAll(zygosities, doFullCount ? Int16.MaxValue : thresh, Convert.ToInt16),
            Int32 thresh => ComputeCountsAll(zygosities, doFullCount ? Int32.MaxValue : thresh, Convert.ToInt32),
            Int64 thresh => ComputeCountsAll(zygosities, doFullCount ? Int64.MaxValue : thresh, Convert.ToInt64),
            _ => throw new ArgumentException($"The type parameter {nameof(OH)} must be one of the types {typeof(byte)}, {typeof(UInt16)},{typeof(UInt32)},{typeof(UInt64)},{typeof(Int16)},{typeof(Int32)},or {typeof(Int64)}, but the type {typeof(OH)} was given.", nameof(OH)),
        });
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
    public static OH[,] ComputeCountsAll<OH>(IReadOnlyList<(BitArrayL32 AA, BitArrayL32 BB)> zygosities, OH threshold, Func<int, OH> convert)
        where OH : unmanaged
    {
        int markerCount = zygosities[0].AA.Count;
        int thresholdInt = Convert.ToInt32(threshold);

        /// Whether we should ignore the threshold and just do a full count or not
        bool ignoreThreshold = thresholdInt * 32 > markerCount; //Very approximate

        OH[,] counts = new OH[zygosities.Count, zygosities.Count];

        // PERF: This does not evenly distribute the work load across the threads.
        Parallel.For(0, zygosities.Count - 1, i1 =>
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
        });
        return counts;
    }

    #region old implementation
    public static OH[,] CountAll<OH>(IReadOnlyList<Zygosity[]> zygosities, OH threshold, bool doFullCount = false)
        where OH : unmanaged
        => CountAll(zygosities.Select((cs, i) => (cs, i)).ToDictionary(x => x.i, x => x.cs), threshold, doFullCount);


    public static OH[,] CountAll<ID, OH>(IDictionary<ID, Zygosity[]> zygosities, OH threshold, bool doFullCount = false)
        where OH : unmanaged
    {
        ID[]? ids = zygosities.Keys.ToArray();
        return Count(zygosities, ids, ids, threshold, doFullCount);
    }

    public static OH[,] Count<ID, OH>(IDictionary<ID, Zygosity[]> zygosities, IReadOnlyList<ID> group2, IReadOnlyList<ID> group1, OH ohThreshold, bool doFullCount = false)
        where OH : unmanaged
    {
        if (zygosities.Count == 0)
            throw new ArgumentException("Zygosity dictionary is empty.", nameof(zygosities));
        if (group1.Count == 0)
            throw new ArgumentException("Group 1 is empty.", nameof(group1));
        if (group2.Count == 0)
            throw new ArgumentException("Group 2 is empty.", nameof(group2));

        // Count opposite homozygotes based on
        return (ohThreshold switch
        {
            byte tresh => CountOh(zygosities, group1, group2, doFullCount ? byte.MaxValue : tresh, Convert.ToByte) as OH[,],
            UInt16 tresh => CountOh(zygosities, group1, group2, doFullCount ? UInt16.MaxValue : tresh, Convert.ToUInt16) as OH[,],
            UInt32 tresh => CountOh(zygosities, group1, group2, doFullCount ? UInt32.MaxValue : tresh, Convert.ToUInt32) as OH[,],
            UInt64 tresh => CountOh(zygosities, group1, group2, doFullCount ? UInt64.MaxValue : tresh, Convert.ToUInt64) as OH[,],
            Int16 tresh => CountOh(zygosities, group1, group2, doFullCount ? Int16.MaxValue : tresh, Convert.ToInt16) as OH[,],
            Int32 tresh => CountOh(zygosities, group1, group2, doFullCount ? Int32.MaxValue : tresh, Convert.ToInt32) as OH[,],
            Int64 tresh => CountOh(zygosities, group1, group2, doFullCount ? Int64.MaxValue : tresh, Convert.ToInt64) as OH[,],
            _ => throw new ArgumentException($"The type parameter {nameof(OH)} must be one of the types {typeof(byte)}, {typeof(UInt16)},{typeof(UInt32)},{typeof(UInt64)},{typeof(Int16)},{typeof(Int32)},or {typeof(Int64)}, but the type {typeof(OH)} was given.", nameof(OH)),
        })!;
    }


    static OH[,] CountOh<ID, OH>(IDictionary<ID, Zygosity[]> zygosities, IReadOnlyList<ID> group1, IReadOnlyList<ID> group2, OH threshold, Func<int, OH> convert)
       where OH : unmanaged
    {
        var group2Masks = CreateHomozygosityMasks(zygosities, group2);

        int markerCount = zygosities.Values.First().Length;
        int thresholdInt = Convert.ToInt32(threshold);

        bool ignoreThreshold = thresholdInt * 32 > markerCount; //Very approximate

        OH[,] counts = new OH[group1.Count, group2.Count];

        Parallel.ForEach(Partitioner.Create(0, group1.Count), (range, _) =>
        {
            BitArrayL32 AA1 = new BitArrayL32(markerCount), BB1 = new BitArrayL32(markerCount);
            for (int i1 = range.Item1; i1 < range.Item2; i1++)
            {
                // Set up bit arrays
                if (i1 > range.Item1)
                {
                    AA1.Clear();
                    BB1.Clear();
                }
                SetHomozygosityMasks(zygosities[group1[i1]], AA1, BB1);

                // Count
                for (int i2 = 0; i2 < group2.Count; i2++)
                {
                    (var AA2, var BB2) = group2Masks[i2];

                    int sum = ignoreThreshold ?
                        VectorOperations.IntersectionCount2(AA1.Words, BB2.Words, AA2.Words, BB1.Words) :
                        VectorOperations.IntersectionCount2(AA1.Words, BB2.Words, AA2.Words, BB1.Words, thresholdInt); // early termination

                    counts[i1, i2] = convert(Math.Min(sum, thresholdInt));
                }
            }
        });
        return counts;
    }


    static readonly Vector<byte> vAA = new Vector<byte>((byte)Zygosity.AA);
    static readonly Vector<byte> vBB = new Vector<byte>((byte)Zygosity.BB);
    /// <summary>
    /// Sets a pair of homozygosity masks (AA, BB) based on an array of zygosity values.
    /// </summary>
    /// <remarks>
    /// The masks are assumed to have sufficient capacity and to be set to zero beforehand.
    /// </remarks>
    /// <param name="zygosities">The array of zygosity values.</param>
    /// <param name="AA">The homozygous AA mask bitarray. This mask will be <see cref="true"/> at the <c>i</c>'th position if and only if <c><paramref name="zygosities"/>[i]</c> is <see cref="Zygosity.AA"/>.</param>
    /// <param name="BB">The homozygous BB mask bitarray. This mask will be <see cref="true"/> at the <c>i</c>'th position if and only if <c><paramref name="zygosities"/>[i]</c> is <see cref="Zygosity.BB"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void SetHomozygosityMasks(Zygosity[] zygosities, BitArrayL32 AA, BitArrayL32 BB)
    {
        for (int i = 0; i < zygosities.Length; i++)
        {
            if (zygosities[i] == Zygosity.AA)
                AA.Add(i);
            else if (zygosities[i] == Zygosity.BB)
                BB.Add(i);
        }
    }

    /// <summary>
    /// Creates an array of homozygosity mask pairs from a dictionary of <paramref name="zygosities"/> and a list of specified sample <paramref name="ids"/>.
    /// The ordering of the masks matches the ordering of the <paramref name="ids"/>.
    /// </summary>
    /// <typeparam name="ID">The type of sample ids.</typeparam>
    /// <param name="zygosities">A dictionary of zygosities to create masks from.</param>
    /// <param name="ids">A list of sample ids to create masks for. The ordering of the return array matches the ordering in this list.</param>
    /// <returns>An array of homozygosity masks of the specified samples.</returns>
    static (BitArrayL32 AA, BitArrayL32 BB)[] CreateHomozygosityMasks<ID>(IDictionary<ID, Zygosity[]> zygosities, IReadOnlyList<ID> ids)
    {
        var masks = new (BitArrayL32 AA, BitArrayL32 BB)[ids.Count];
        Parallel.ForEach(Partitioner.Create(0, ids.Count), (range, _) =>
        {
            for (int i = range.Item1; i < range.Item2; i++)
            {
                Zygosity[] z = zygosities[ids[i]];
                BitArrayL32 AA = new(z.Length), BB = new BitArrayL32(z.Length);
                SetHomozygosityMasks(z, AA, BB);
                masks[i] = (AA, BB);
            }
        });
        return masks;
    }
    #endregion old implementation
}
