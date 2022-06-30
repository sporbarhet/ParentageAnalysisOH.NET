using Microsoft.Toolkit.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;


namespace Sporbarhet.Parentage.BitCollections;
/// <summary>
/// This is a collection of methods that involve using vectorized (SIMD) instructions.
/// <br/>
/// See <see cref="HarleySeal"/> for methods relating to the Harley-Seal population count algorithm.
/// </summary>
public static class VectorOperations
{
    #region population counts

    /// <summary>
    /// Computes the population count of a <see cref="Vector{T}"/>.
    /// The population count of a binary value is the number of <c>1</c>'s in that value.
    /// </summary>
    /// <param name="v">The input <see cref="Vector{T}"/> to compute the population count of.</param>
    /// <returns>the population count of the input vector as a <see cref="Int32"/></returns>
    /// <exception cref="ArgumentException">An argument exception is thrown if the word count of the input vector is not 8, 4, 2, or 1.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(Vector<ulong> v)// => BitOperations.PopCount(v[0]) + BitOperations.PopCount(v[1]) + BitOperations.PopCount(v[2]) + BitOperations.PopCount(v[3]);
        => Vector<ulong>.Count switch // Branching is optimized away at runtime (hopefully)
        {
            4 => BitOperations.PopCount(v[0]) + BitOperations.PopCount(v[1]) + BitOperations.PopCount(v[2]) + BitOperations.PopCount(v[3]),
            8 => BitOperations.PopCount(v[0]) + BitOperations.PopCount(v[1]) + BitOperations.PopCount(v[2]) + BitOperations.PopCount(v[3])
                + BitOperations.PopCount(v[4]) + BitOperations.PopCount(v[5]) + BitOperations.PopCount(v[6]) + BitOperations.PopCount(v[7]),
            2 => BitOperations.PopCount(v[0]) + BitOperations.PopCount(v[1]),
            1 => BitOperations.PopCount(v[0]),
            _ => ThrowHelper.ThrowArgumentException<int>("Unsupported vector word count.")
        };

    /// <summary>
    /// Computes the population count of a <see cref="Vector256{T}"/>.
    /// The population count of a binary value is the number of <c>1</c>'s in that value.
    /// </summary>
    /// <param name="v">The input <see cref="Vector256{T}"/> to compute the population count of.</param>
    /// <returns>the population count of the input vector as a <see cref="Int32"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(Vector256<ulong> v)
        => BitOperations.PopCount(v.GetElement(0)) + BitOperations.PopCount(v.GetElement(1)) + BitOperations.PopCount(v.GetElement(2)) + BitOperations.PopCount(v.GetElement(3));

    /// <summary>
    /// Computes the population count of a <see cref="Vector128{T}"/>.
    /// The population count of a binary value is the number of <c>1</c>'s in that value.
    /// </summary>
    /// <param name="v">The input <see cref="Vector128{T}"/> to compute the population count of.</param>
    /// <returns>the population count of the input vector as a <see cref="Int32"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(Vector128<ulong> v)
        => BitOperations.PopCount(v.GetElement(0)) + BitOperations.PopCount(v.GetElement(1));

    /// <summary>
    /// Computes the population counts of a <see cref="Vector256{T}"/>, subdivided into words of 64 bits.
    /// The population count of a binary value is the number of <c>1</c>'s in that value.
    /// </summary>
    /// <param name="v">The input <see cref="Vector256{T}"/> to compute the population counts of.</param>
    /// <returns>the population counts of the input vector <see cref="Vector256{T}"/> as a <see cref="Vector128{T}"/> of <see cref="Int32"/>s.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> PopCounts(Vector256<ulong> v)
        => Vector128.Create(BitOperations.PopCount(v.GetElement(0)), BitOperations.PopCount(v.GetElement(1)), BitOperations.PopCount(v.GetElement(2)), BitOperations.PopCount(v.GetElement(3)));

    #endregion population counts




    #region intersection counting

    /// <summary>
    /// Counts the population of the intersection of <paramref name="A0"/> with <paramref name="B0"/> union the intersection of <paramref name="A1"/> with <paramref name="B1"/>:
    /// <c>(<paramref name="A0"/> &amp; <paramref name="B0"/>) | (<paramref name="A1"/> &amp; <paramref name="B1"/>)</c>.
    /// </summary>
    /// <remarks>
    /// Under the assumption that the intersections are disjoint (which is true for the opposing homozygous loci count),
    /// this is equal to the population count of <paramref name="A0"/> intersect <paramref name="B0"/> plus the
    /// population count of <paramref name="A1"/> intersect <paramref name="B1"/>, but is considerably faster as it
    /// only performs half the number of <see cref="BitOperations.PopCount(ulong)"/> calls.
    /// </remarks>
    /// <param name="A0">The first  span of bits. This will be intersected/logically anded with <paramref name="B0"/>.</param>
    /// <param name="B0">The second span of bits. This will be intersected/logically anded with <paramref name="A0"/>.</param>
    /// <param name="A1">The third  span of bits. This will be intersected/logically anded with <paramref name="B1"/>.</param>
    /// <param name="B1">The fourth span of bits. This will be intersected/logically anded with <paramref name="A1"/>.</param>
    /// <returns>The population count of <c>(<paramref name="A0"/> &amp; <paramref name="B0"/>) | (<paramref name="A1"/> &amp; <paramref name="B1"/>)</c>.</returns>
    public static unsafe int IntersectionCount2(ReadOnlySpan<ulong> A0, ReadOnlySpan<ulong> B0, ReadOnlySpan<ulong> A1, ReadOnlySpan<ulong> B1)
    {
        if (A0.Length != B0.Length || A0.Length != A1.Length || A0.Length != B1.Length)
            ThrowHelper.ThrowArgumentException("Array length mismatch");
        fixed (ulong* pA0 = A0, pB0 = B0, pA1 = A1, pB1 = B1)
            return IntersectionCount2(pA0, pB0, pA1, pB1, A0.Length);
    }

    /// <summary>
    /// Counts the population of the intersection of <paramref name="A0"/> with <paramref name="B0"/> union the intersection of <paramref name="A1"/> with <paramref name="B1"/>:
    /// <c>(<paramref name="A0"/> &amp; <paramref name="B0"/>) | (<paramref name="A1"/> &amp; <paramref name="B1"/>)</c>.
    /// If the count exceeds the <paramref name="threshold"/>, the counting is terminated and the current value returned.
    /// </summary>
    /// <remarks>
    /// Under the assumption that the intersections are disjoint (which is true for the opposing homozygous loci count),
    /// this is equal to the population count of <paramref name="A0"/> intersect <paramref name="B0"/> plus the
    /// population count of <paramref name="A1"/> intersect <paramref name="B1"/>, but is considerably faster as it
    /// only performs half the number of <see cref="BitOperations.PopCount(ulong)"/> calls.
    /// </remarks>
    /// <param name="A0">The first  span of bits. This will be intersected/logically anded with <paramref name="B0"/>.</param>
    /// <param name="B0">The second span of bits. This will be intersected/logically anded with <paramref name="A0"/>.</param>
    /// <param name="A1">The third  span of bits. This will be intersected/logically anded with <paramref name="B1"/>.</param>
    /// <param name="B1">The fourth span of bits. This will be intersected/logically anded with <paramref name="A1"/>.</param>
    /// <param name="threshold">The counting threshold. If the true count exceeds this value, the counting is terminated and the current value returned.</param>
    /// <returns>The population count of <c>(<paramref name="A0"/> &amp; <paramref name="B0"/>) | (<paramref name="A1"/> &amp; <paramref name="B1"/>)</c>, or a value above or equal to <paramref name="threshold"/> if the true count exceeds this.</returns>
    public static unsafe int IntersectionCount2(ReadOnlySpan<ulong> A0, ReadOnlySpan<ulong> B0, ReadOnlySpan<ulong> A1, ReadOnlySpan<ulong> B1, int threshold)
    {
        if (A0.Length != B0.Length || A0.Length != A1.Length || A0.Length != B1.Length)
            ThrowHelper.ThrowArgumentException("Array length mismatch");

        fixed (ulong* pA0 = A0, pB0 = B0, pA1 = A1, pB1 = B1) // Force omit bounds checks
            return IntersectionCount2(pA0, pB0, pA1, pB1, A0.Length, threshold);
    }


    /// <summary>
    /// Counts the population of the intersection of <paramref name="pA0"/> with <paramref name="pB0"/> union the intersection of <paramref name="pA1"/> with <paramref name="pB1"/>:
    /// <c>(<paramref name="pA0"/> &amp; <paramref name="pB0"/>) | (<paramref name="pA1"/> &amp; <paramref name="pB1"/>)</c>.
    /// </summary>
    /// <remarks>
    /// Under the assumption that the intersections are disjoint (which is true for the opposing homozygous loci count),
    /// this is equal to the population count of <paramref name="pA0"/> intersect <paramref name="pB0"/> plus the
    /// population count of <paramref name="pA1"/> intersect <paramref name="pB1"/>, but is considerably faster as it
    /// only performs half the number of <see cref="BitOperations.PopCount(ulong)"/> calls.
    /// </remarks>
    /// <param name="pA0">The first region of bits.  This will be intersected/logically anded with <paramref name="pB0"/>.</param>
    /// <param name="pB0">The second region of bits. This will be intersected/logically anded with <paramref name="pA0"/>.</param>
    /// <param name="pA1">The third region of bits.  This will be intersected/logically anded with <paramref name="pB1"/>.</param>
    /// <param name="pB1">The fourth region of bits. This will be intersected/logically anded with <paramref name="pA1"/>.</param>
    /// <param name="length">The length of each region of bits.</param>
    /// <returns>The population count of <c>(<paramref name="pA0"/> &amp; <paramref name="pB0"/>) | (<paramref name="pA1"/> &amp; <paramref name="pB1"/>)</c>.</returns>
    static unsafe int IntersectionCount2(ulong* pA0, ulong* pB0, ulong* pA1, ulong* pB1, int length)
    {
        int i = 0, count = 0;
        if (Avx2.IsSupported)
        {
            int c = Vector256<ulong>.Count, iLim = length - 2 * c + 1;
            for (; i < iLim; i += 2 * c) // Unrolled to halve bounds checks.
                count += PopCount(Avx2.Or(
                        Avx2.And(Avx.LoadVector256(&pA0[i]), Avx.LoadVector256(&pB0[i])),
                        Avx2.And(Avx.LoadVector256(&pA1[i]), Avx.LoadVector256(&pB1[i]))))
                       + PopCount(Avx2.Or(
                        Avx2.And(Avx.LoadVector256(&pA0[i + c]), Avx.LoadVector256(&pB0[i + c])),
                        Avx2.And(Avx.LoadVector256(&pA1[i + c]), Avx.LoadVector256(&pB1[i + c]))));
        }
        if (Sse2.IsSupported)
        {
            int c = Vector128<ulong>.Count, iLim = length - c + 1;
            for (; i < iLim; i += c)
                count += PopCount(Sse2.Or(
                        Sse2.And(Sse2.LoadVector128(&pA0[i]), Sse2.LoadVector128(&pB0[i])),
                        Sse2.And(Sse2.LoadVector128(&pA1[i]), Sse2.LoadVector128(&pB1[i]))));
        }

        // Count tail elements that didn't fit in a vector
        for (; i < length; i++)
            count += BitOperations.PopCount(pA0[i] & pB0[i] | pA1[i] & pB1[i]);

        return count;
    }

    /// <summary>
    /// Counts the population of the intersection of <paramref name="pA0"/> with <paramref name="pB0"/> union the intersection of <paramref name="pA1"/> with <paramref name="pB1"/>:
    /// <c>(<paramref name="pA0"/> &amp; <paramref name="pB0"/>) | (<paramref name="pA1"/> &amp; <paramref name="pB1"/>)</c>.
    /// If the count exceeds the <paramref name="threshold"/>, the counting is terminated and the current value returned.
    /// </summary>
    /// <remarks>
    /// Under the assumption that the intersections are disjoint (which is true for the opposing homozygous loci count),
    /// this is equal to the population count of <paramref name="pA0"/> intersect <paramref name="pB0"/> plus the
    /// population count of <paramref name="pA1"/> intersect <paramref name="pB1"/>, but is considerably faster as it
    /// only performs half the number of <see cref="BitOperations.PopCount(ulong)"/> calls.
    /// </remarks>
    /// <param name="pA0">The first region of bits.  This will be intersected/logically anded with <paramref name="pB0"/>.</param>
    /// <param name="pB0">The second region of bits. This will be intersected/logically anded with <paramref name="pA0"/>.</param>
    /// <param name="pA1">The third region of bits.  This will be intersected/logically anded with <paramref name="pB1"/>.</param>
    /// <param name="pB1">The fourth region of bits. This will be intersected/logically anded with <paramref name="pA1"/>.</param>
    /// <param name="length">The length of each region of bits.</param>
    /// <param name="threshold">The counting threshold. If the true count exceeds this value, the counting is terminated and the current value returned.</param>
    /// <returns>The population count of <c>(<paramref name="pA0"/> &amp; <paramref name="pB0"/>) | (<paramref name="pA1"/> &amp; <paramref name="pB1"/>)</c>, or a value above or equal to <paramref name="threshold"/> if the true count exceeds this.</returns>
    static unsafe int IntersectionCount2(ulong* pA0, ulong* pB0, ulong* pA1, ulong* pB1, int length, int threshold)
    {
        int i = 0, count = 0;
        if (Avx2.IsSupported)
        {
            int c = Vector256<ulong>.Count, iLim = length - 2 * c + 1;
            for (; i < iLim && count < threshold; i += 2 * c) // Unrolled to halve bounds checks.
                count += PopCount(Avx2.Or(
                        Avx2.And(Avx.LoadVector256(&pA0[i]), Avx.LoadVector256(&pB0[i])),
                        Avx2.And(Avx.LoadVector256(&pA1[i]), Avx.LoadVector256(&pB1[i]))))
                       + PopCount(Avx2.Or(
                        Avx2.And(Avx.LoadVector256(&pA0[i + c]), Avx.LoadVector256(&pB0[i + c])),
                        Avx2.And(Avx.LoadVector256(&pA1[i + c]), Avx.LoadVector256(&pB1[i + c]))));
            if (count >= threshold)
                return count;
        }
        if (Sse2.IsSupported)
        {
            int c = Vector128<ulong>.Count, iLim = length - c + 1;
            for (; i < iLim && count < threshold; i += c)
                count += PopCount(Sse2.Or(
                        Sse2.And(Sse2.LoadVector128(&pA0[i]), Sse2.LoadVector128(&pB0[i])),
                        Sse2.And(Sse2.LoadVector128(&pA1[i]), Sse2.LoadVector128(&pB1[i]))));
            if (count >= threshold)
                return count;
        }

        // Count tail elements that didn't fit in a vector
        for (; i < length; i++)
            count += BitOperations.PopCount(pA0[i] & pB0[i] | pA1[i] & pB1[i]);

        return count;
    }
    #endregion intersection counting
}
