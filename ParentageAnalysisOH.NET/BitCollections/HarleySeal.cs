using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Sporbarhet.Parentage.BitCollections;
public static class HarleySeal
{
    public static int IntersectionCount2(ReadOnlySpan<ulong> A0, ReadOnlySpan<ulong> B0, ReadOnlySpan<ulong> A1, ReadOnlySpan<ulong> B1)
    {
        //Vector256<UInt64>.Count * 16 is the atomic step size of VectorOperations.IntersectionCount
        int nose = A0.Length % (Vector256<ulong>.Count * 16);
        return VectorOperations.IntersectionCount2(A0[..nose], B0[..nose], A1[..nose], B1[..nose]) + IntersectionCount2<ulong>(A0[nose..], B0[nose..], A1[nose..], B1[nose..]);
    }

    /// <summary>

    /// </summary>
    /// <remarks>
    /// This is a compromise between early termination and Harley-Seal.
    /// This method may be worthwhile if you expect low counts or have set a high threshold.
    /// </remarks>
    /// <param name="A0"></param>
    /// <param name="B0"></param>
    /// <param name="A1"></param>
    /// <param name="B1"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public static int IntersectionCount2(ReadOnlySpan<ulong> A0, ReadOnlySpan<ulong> B0, ReadOnlySpan<ulong> A1, ReadOnlySpan<ulong> B1, int threshold)
    {
        //Vector256<UInt64>.Count * 16 is the atomic step size of VectorOperations.IntersectionCount
        int nose = A0.Length % (Vector256<ulong>.Count * 16);
        int count = VectorOperations.IntersectionCount2(A0[..nose], B0[..nose], A1[..nose], B1[..nose], threshold);
        if (count < threshold)
            count += IntersectionCount2<ulong>(A0[nose..], B0[nose..], A1[nose..], B1[nose..]);
        return count;
    }


    static unsafe int IntersectionCount2<T>(ReadOnlySpan<T> A0, ReadOnlySpan<T> B0, ReadOnlySpan<T> A1, ReadOnlySpan<T> B1)
        where T : unmanaged
    {
        // Cast to vector pointer
        fixed (T* a0 = A0, b0 = B0, a1 = A1, b1 = B1)
            return (int)IntersectionCount2((Vector256<ulong>*)a0, (Vector256<ulong>*)b0, (Vector256<ulong>*)a1, (Vector256<ulong>*)b1, A0.Length / Vector256<ulong>.Count);
    }


    /// <summary>
    /// Computes the population count of an array using Avx2 instructions.
    /// Implementation of Harley-Seal's algorithm (<see href="https://arxiv.org/pdf/1611.07612.pdf"/>)
    /// </summary>
    /// <param name="vs"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    static unsafe ulong IntersectionCount2(Vector256<ulong>* a0, Vector256<ulong>* b0, Vector256<ulong>* a1, Vector256<ulong>* b1, int length)
    {
        Vector256<ulong> total = Vector256<ulong>.Zero,//_mm256_setzero_si256()
        ones = Vector256<ulong>.Zero,//_mm256_setzero_si256()
        twos = Vector256<ulong>.Zero,//_mm256_setzero_si256()
        fours = Vector256<ulong>.Zero,//_mm256_setzero_si256()
        eights = Vector256<ulong>.Zero;//_mm256_setzero_si256()
        for (int i = 0; i < length - 15; i += 16)
        {
            CSA(out Vector256<ulong> twosA, out ones, ones, Avx2.Or(Avx2.And(a0[i], b0[i]), Avx2.And(a1[i], b1[i])), Avx2.Or(Avx2.And(a0[i + 1], b0[i + 1]), Avx2.And(a1[i + 1], b1[i + 1])));
            CSA(out Vector256<ulong> twosB, out ones, ones, Avx2.Or(Avx2.And(a0[i + 2], b0[i + 2]), Avx2.And(a1[i + 2], b1[i + 2])), Avx2.Or(Avx2.And(a0[i + 3], b0[i + 3]), Avx2.And(a1[i + 3], b1[i + 3])));
            CSA(out Vector256<ulong> foursA, out twos, twos, twosA, twosB);
            CSA(out twosA, out ones, ones, Avx2.Or(Avx2.And(a0[i + 4], b0[i + 4]), Avx2.And(a1[i + 4], b1[i + 4])), Avx2.Or(Avx2.And(a0[i + 5], b0[i + 5]), Avx2.And(a1[i + 5], b1[i + 5])));
            CSA(out twosB, out ones, ones, Avx2.Or(Avx2.And(a0[i + 6], b0[i + 6]), Avx2.And(a1[i + 6], b1[i + 6])), Avx2.Or(Avx2.And(a0[i + 7], b0[i + 7]), Avx2.And(a1[i + 7], b1[i + 7])));
            CSA(out Vector256<ulong> foursB, out twos, twos, twosA, twosB);
            CSA(out Vector256<ulong> eightsA, out fours, fours, foursA, foursB);
            CSA(out twosA, out ones, ones, Avx2.Or(Avx2.And(a0[i + 8], b0[i + 8]), Avx2.And(a1[i + 8], b1[i + 8])), Avx2.Or(Avx2.And(a0[i + 9], b0[i + 9]), Avx2.And(a1[i + 9], b1[i + 9])));
            CSA(out twosB, out ones, ones, Avx2.Or(Avx2.And(a0[i + 10], b0[i + 10]), Avx2.And(a1[i + 10], b1[i + 10])), Avx2.Or(Avx2.And(a0[i + 11], b0[i + 11]), Avx2.And(a1[i + 11], b1[i + 11])));
            CSA(out foursA, out twos, twos, twosA, twosB);
            CSA(out twosA, out ones, ones, Avx2.Or(Avx2.And(a0[i + 12], b0[i + 12]), Avx2.And(a1[i + 12], b1[i + 12])), Avx2.Or(Avx2.And(a0[i + 13], b0[i + 13]), Avx2.And(a1[i + 13], b1[i + 13])));
            CSA(out twosB, out ones, ones, Avx2.Or(Avx2.And(a0[i + 14], b0[i + 14]), Avx2.And(a1[i + 14], b1[i + 14])), Avx2.Or(Avx2.And(a0[i + 15], b0[i + 15]), Avx2.And(a1[i + 15], b1[i + 15])));
            CSA(out foursB, out twos, twos, twosA, twosB);
            CSA(out Vector256<ulong> eightsB, out fours, fours, foursA, foursB);
            CSA(out Vector256<ulong> sixteens, out eights, eights, eightsA, eightsB);
            total = Avx2.Add(total, Count(sixteens));//_mm256_add_epi64(total, count(sixteens))
        }
        total = Avx2.ShiftLeftLogical(total, 4);//_mm256_slli_epi64(total, 4)
        total = Avx2.Add(total, Avx2.ShiftLeftLogical(Count(eights), 3));//_mm256_add_epi64(total, _mm256_slli_epi64(count(eights), 3))
        total = Avx2.Add(total, Avx2.ShiftLeftLogical(Count(fours), 2));//_mm256_add_epi64(total, _mm256_slli_epi64(count(fours), 2))
        total = Avx2.Add(total, Avx2.ShiftLeftLogical(Count(twos), 1));//_mm256_add_epi64(total, _mm256_slli_epi64(count(twos), 1))
        total = Avx2.Add(total, Count(ones));//_mm256_add_epi64(total, count(ones))
        //return _mm256_extract_epi64(total, 0) + _mm256_extract_epi64(total, 1) + _mm256_extract_epi64(total, 2) + _mm256_extract_epi64(total , 3);
        return total.GetElement(0) + total.GetElement(1) + total.GetElement(2) + total.GetElement(3);
    }




    /// <summary>
    /// Computes the population count of an array using Avx2 instructions.
    /// Implementation of Harley-Seal's algorithm (<see href="https://arxiv.org/pdf/1611.07612.pdf"/>)
    /// </summary>
    /// <param name="vs"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    static unsafe ulong PopCountHS(Vector256<ulong>* d, int length)
    {
        Vector256<ulong> total = Vector256<ulong>.Zero,//_mm256_setzero_si256()
        ones = Vector256<ulong>.Zero,//_mm256_setzero_si256()
        twos = Vector256<ulong>.Zero,//_mm256_setzero_si256()
        fours = Vector256<ulong>.Zero,//_mm256_setzero_si256()
        eights = Vector256<ulong>.Zero;//_mm256_setzero_si256()
        for (int i = 0; i < length - 15; i += 16)
        {
            CSA(out Vector256<ulong> twosA, out ones, ones, d[i + 0], d[i + 1]);
            CSA(out Vector256<ulong> twosB, out ones, ones, d[i + 2], d[i + 3]);
            CSA(out Vector256<ulong> foursA, out twos, twos, twosA, twosB);
            CSA(out twosA, out ones, ones, d[i + 4], d[i + 5]);
            CSA(out twosB, out ones, ones, d[i + 6], d[i + 7]);
            CSA(out Vector256<ulong> foursB, out twos, twos, twosA, twosB);
            CSA(out Vector256<ulong> eightsA, out fours, fours, foursA, foursB);
            CSA(out twosA, out ones, ones, d[i + 8], d[i + 9]);
            CSA(out twosB, out ones, ones, d[i + 10], d[i + 11]);
            CSA(out foursA, out twos, twos, twosA, twosB);
            CSA(out twosA, out ones, ones, d[i + 12], d[i + 13]);
            CSA(out twosB, out ones, ones, d[i + 14], d[i + 15]);
            CSA(out foursB, out twos, twos, twosA, twosB);
            CSA(out Vector256<ulong> eightsB, out fours, fours, foursA, foursB);
            CSA(out Vector256<ulong> sixteens, out eights, eights, eightsA, eightsB);
            total = Avx2.Add(total, Count(sixteens));//_mm256_add_epi64(total, count(sixteens))
        }

        total = Avx2.ShiftLeftLogical(total, 4);//_mm256_slli_epi64(total, 4)
        total = Avx2.Add(total, Avx2.ShiftLeftLogical(Count(eights), 3));//_mm256_add_epi64(total, _mm256_slli_epi64(count(eights), 3))
        total = Avx2.Add(total, Avx2.ShiftLeftLogical(Count(fours), 2));//_mm256_add_epi64(total, _mm256_slli_epi64(count(fours), 2))
        total = Avx2.Add(total, Avx2.ShiftLeftLogical(Count(twos), 1));//_mm256_add_epi64(total, _mm256_slli_epi64(count(twos), 1))
        total = Avx2.Add(total, Count(ones));//_mm256_add_epi64(total, count(ones))
        //return _mm256_extract_epi64(total, 0) + _mm256_extract_epi64(total, 1) + _mm256_extract_epi64(total, 2) + _mm256_extract_epi64(total , 3);
        return total.GetElement(0) + total.GetElement(1) + total.GetElement(2) + total.GetElement(3);
    }

    /// <summary>
    /// Carry save adder.
    /// </summary>
    /// <param name="h"></param>
    /// <param name="l"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void CSA(out Vector256<ulong> h, out Vector256<ulong> l, Vector256<ulong> a, Vector256<ulong> b, Vector256<ulong> c)
    {
        Vector256<ulong> u = Avx2.Xor(a , b);
        h = Avx2.Or(Avx2.And(a, b), Avx2.And(u, c));//_mm256_or_si256(_mm256_and_si256(a, b), _mm256_and_si256(u, c))
        l = Avx2.Xor(u, c);
    }

    static readonly Vector256<byte> count_lookup = Vector256.Create((byte)//_mm256_setr_epi8(...)
        0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4,
        0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4
    );

    static Vector256<ulong> Count(Vector256<ulong> v)
    {
        Vector256<uint> low_mask = Vector256.Create((byte)0x0f).AsUInt32();//_mm256_set1_epi8(0x0f)
        Vector256<byte> lo = Avx2.And(v.AsUInt32(), low_mask).AsByte();//_mm256_and_si256(v, low_mask)
        Vector256<byte> hi = Avx2.And(Avx2.ShiftRightLogical(v.AsUInt32(), 4), low_mask).AsByte();//_mm256_and_si256(_mm256_srli_epi32(v, 4), low_mask)
        Vector256<byte> popcnt1 = Avx2.Shuffle(count_lookup, lo);//_mm256_shuffle_epi8(lookup, lo)
        Vector256<byte> popcnt2 = Avx2.Shuffle(count_lookup, hi);//_mm256_shuffle_epi8(lookup, hi)
        Vector256<byte> total = Avx2.Add(popcnt1, popcnt2);//_mm256_add_epi8(popcnt1, popcnt2)
        return Avx2.SumAbsoluteDifferences(total.AsByte(), Vector256<byte>.Zero).AsUInt64();//_mm256_sad_epu8(total, _mm256_setzero_si256())
    }
}
