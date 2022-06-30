using Microsoft.Toolkit.Diagnostics;
using Sporbarhet.Parentage.BitCollections.Extensions;
using System.Collections;
using System.Numerics;
using TInd = System.Int32;
using TWord = System.UInt64;

namespace Sporbarhet.Parentage.BitCollections;

/// <summary>
/// An array of bits organized contiguously in memory divided into <see cref="TWord"/> words and indexed by <see cref="TInd"/>s.
/// </summary>
/// <remarks>
/// The number 32 in the name refers to the bit size of the index space. See <see cref="TInd"/>.
/// The L refers to the fact that the internal word size is the same as a long integer (64 bits). See <see cref="TWord"/>.
/// </remarks>
public struct BitArrayL32 : IBitArray<TInd, TWord, BitArrayL32>, IEnumerable<TInd>, IEnumerable<bool>, IEquatable<BitArrayL32>
{
    public TWord[] Words { get; }

    public TInd Count => Words.Length << BIT_ADDR_SIZE;

    // Constants
    const TWord ZERO = 0;
    const TWord ONE = 1;
    const TWord ALL_BITS = ~default(TWord);
    const int BITS_PER_WORD = sizeof(TWord) * 8;
    const int BIT_ADDR_SIZE = 6; // BitOperations.Log2(BITS_PER_WORD);
    /// <summary>
    /// Mask for extracting the bits addressing a bit within a single position of the internal array.
    /// E.g. when <see cref="BIT_ADDR_SIZE"/> is 6, this mask will be 0b111111.
    /// </summary>
    const int BIT_ADDR_MASK = (1 << BIT_ADDR_SIZE) - 1;
    const TWord MAX_BIT = ONE << BITS_PER_WORD - 1;

    public bool IsReadOnly => Words.IsReadOnly;
    public object SyncRoot => Words.SyncRoot;
    public bool IsFixedSize => Words.IsFixedSize;

    /// <summary>
    /// Creates a new <see cref="BitArrayL32"/> with a capacity of at least <paramref name="bitCapacity"/>.
    /// </summary>
    /// <param name="bitCapacity">The required bit capacity of the <see cref="BitArrayL32"/>.</param>
    /// <remarks>
    /// The capacity is rounded up to the nearest whole word, so do not expect the <see cref="Count"/> of this <see cref="BitArrayL32"/> to be exactly equal to the given <paramref name="bitCapacity"/>.
    /// </remarks>
    public BitArrayL32(TInd bitCapacity)
    {
        int wordsLength = (bitCapacity + BITS_PER_WORD - 1) / BITS_PER_WORD;
        Words = new TWord[wordsLength];
    }

    /// <summary>
    /// Creates a new <see cref="BitArrayL32"/> with a capacity of at least <paramref name="bitCapacity"/> and fills it with <paramref name="fillValue"/>.
    /// </summary>
    /// <param name="bitCapacity">The required bit capacity of the <see cref="BitArrayL32"/>.</param>
    /// <param name="fillValue">The fill value to fill this <see cref="BitArrayL32"/> with. All bits in this <see cref="BitArrayL32"/> will be set to the given value, even the bits above the given <paramref name="bitCapacity"/>.</param>
    /// <remarks>
    /// The capacity is rounded up to the nearest whole word, so do not expect the <see cref="Count"/> of this <see cref="BitArrayL32"/> to be exactly equal to the given <paramref name="bitCapacity"/>.
    /// </remarks>
    public BitArrayL32(TInd bitCapacity, bool fillValue)
        : this(bitCapacity)
    {
        if (fillValue)
            SetAll(true);
    }

    /// <summary>
    /// Creates a new <see cref="BitArrayL32"/> with a capacity of at least <c><paramref name="bits"/>.Count</c> and fills it with the values of <paramref name="bits"/>.
    /// </summary>
    /// <param name="bits">A collection of initial bits to set in this <see cref="BitArrayL32"/>. Its <see cref="IReadOnlyCollection{T}.Count"/> is also used to determine the capacity.</param>
    /// <remarks>
    /// The capacity is rounded up to the nearest whole word, so do not expect the <see cref="Count"/> of this <see cref="BitArrayL32"/> to be exactly equal to the given <see cref="IReadOnlyCollection{T}.Count"/>.
    /// </remarks>
    public BitArrayL32(IReadOnlyCollection<bool> bits)
        : this(bits.Count)
    {
        int wordInd = 0, bitInd = 0;
        foreach (bool bit in bits)
        {
            if (bit)
                SetTrue(wordInd, bitInd);

            if (++bitInd >= BITS_PER_WORD)
            {
                bitInd = 0;
                wordInd++;
            }
        }
    }

    /// <summary>
    /// Creates a new <see cref="BitArrayL32"/> with the given array of words.
    /// </summary>
    /// <param name="words">An array of words to use in this <see cref="BitArrayL32"/>.</param>
    /// <remarks>
    /// The array is copied by reference, so the <see cref="BitArrayL32"/> takes ownership over the passed array.
    /// </remarks>
    public BitArrayL32(TWord[] words) => Words = words;


    /// <summary>
    /// Gets the index of the word in which the bit at the given <paramref name="index"/> resides.
    /// </summary>
    /// <param name="index">The index of a bit.</param>
    /// <returns>The array index for the word in which the bit at the given <paramref name="index"/> resides.</returns>
    public static int GetWordIndex(TInd index) => index >> BIT_ADDR_SIZE;

    /// <summary>
    /// Gets the position of the bit at the given <paramref name="index"/> inside of its word.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>The position of the bit at the given <paramref name="index"/> inside of its word.</returns>
    public static int GetBitIndex(TInd index) => unchecked(index & BIT_ADDR_MASK);

    /// <summary>
    /// Gets the global index of a bit given its word index and bit index.
    /// See also <seealso cref="GetWordIndex(TInd)"/> and <seealso cref="GetBitIndex(TInd)"/>.
    /// </summary>
    /// <param name="wordIndex">The array index for the word in which the bit resides.</param>
    /// <param name="bitIndex">The position of the bit in its word.</param>
    /// <returns></returns>
    public static TInd GetItem(int wordIndex, int bitIndex) => wordIndex << BIT_ADDR_SIZE | bitIndex;


    /// <summary>
    /// Sets all bits in this <see cref="BitArrayL32"/> to the given <paramref name="value"/> value.
    /// </summary>
    /// <param name="value">The boolean value to set all bits in this <see cref="BitArrayL32"/> to.</param>
    public void SetAll(bool value)
    {
        if (value)
            Words.Fill(ALL_BITS);
        else
            Words.Clear();
    }


    /// <summary>
    /// Sets all bits in this <see cref="BitArrayL32"/> to <see langword="false"/>.
    /// </summary>
    public void Clear() => Words.Clear();

    /// <summary>
    /// Sets the bit at the given position to <see langword="true"/>.
    /// </summary>
    /// <param name="item">The position of the given bit.</param>
    public void SetTrue(TInd item) => SetTrue(GetWordIndex(item), GetBitIndex(item));

    /// <summary>
    /// Sets the bit at the given position to <see langword="true"/>.
    /// </summary>
    /// <param name="wordIndex">The array index for the word in which the bit resides.</param>
    /// <param name="bitIndex">The position of the bit in its word.</param>
    public void SetTrue(int wordIndex, int bitIndex) => Words[wordIndex] |= ONE << bitIndex;

    /// <summary>
    /// Sets the bit at the given position to <see langword="false"/>.
    /// </summary>
    /// <param name="item">The position of the given bit.</param>
    public void SetFalse(TInd item) => SetFalse(GetWordIndex(item), GetBitIndex(item));

    /// <summary>
    /// Sets the bit at the given position to <see langword="false"/>.
    /// </summary>
    /// <param name="wordIndex">The array index for the word in which the bit resides.</param>
    /// <param name="bitIndex">The position of the bit in its word.</param>
    public void SetFalse(int wordIndex, int bitIndex) => Words[wordIndex] &= ~(ONE << bitIndex);

    /// <inheritdoc/>
    public void Add(TInd item) => SetTrue(item);

    /// <inheritdoc/>
    public bool TryAdd(TInd item)
    {
        if (item >= Words.Length * BITS_PER_WORD || item < 0)
            return false;
        int arrIndex = GetWordIndex(item);
        int bitIndex = GetBitIndex(item);
        bool value = Get(arrIndex, bitIndex);
        SetTrue(arrIndex, bitIndex);
        return !value;
    }

    /// <inheritdoc/>
    public void Remove(TInd item) => SetFalse(item);

    /// <inheritdoc/>
    public bool TryRemove(TInd item)
    {
        if (item >= Count || item < 0)
            return false;
        int arrIndex = GetWordIndex(item);
        int bitIndex = GetBitIndex(item);
        bool value = Get(arrIndex, bitIndex);
        SetFalse(arrIndex, bitIndex);
        return value;
    }

    /// <summary>
    /// Gets the bit at the given position.
    /// </summary>
    /// <param name="wordIndex">The array index for the word in which the bit resides.</param>
    /// <param name="bitIndex">The position of the bit in its word.</param>
    /// <returns>The value of the bit at the given position. Returns <see langword="true"/> if the bit is set, <see langword="false"/> if it is not set.</returns>
    public bool Get(int wordIndex, int bitIndex) => (Words[wordIndex] & ONE << bitIndex) != 0;

    /// <summary>
    /// Sets the bit at the given position to the given <paramref name="value"/>.
    /// </summary>
    /// <param name="wordIndex">The array index for the word in which the bit resides.</param>
    /// <param name="bitIndex">The position of the bit in its word.</param>
    /// <param name="value">The value to set the bit at the given position to.</param>
    public void Set(int wordIndex, int bitIndex, bool value)
    {
        if (value)
            SetTrue(wordIndex, bitIndex);
        else
            SetFalse(wordIndex, bitIndex);
    }

    /// <inheritdoc/>
    public bool this[TInd index] { get => Get(GetWordIndex(index), GetBitIndex(index)); set => Set(GetWordIndex(index), GetBitIndex(index), value); }

    /// <inheritdoc/>
    public bool Contains(TInd item) => this[item];


    public BitArrayL32 Not(BitArrayL32 result)
    {
        if (Count != result.Count)
            ThrowHelper.ThrowArgumentException(nameof(result), $"{nameof(BitArrayL32)} dimension mismatch. This {nameof(BitArrayL32)} has a bit count of {Count}, while {nameof(result)} has a bit count of {result.Count}.");

        int i = 0;
        for (; i < Words.Length - Vector<TWord>.Count; i += Vector<TWord>.Count) // SIMD loop
            (~new Vector<TWord>(Words[i..])).CopyTo(result.Words[i..]);

        for (; i < Words.Length; i++) // SISD loop, tail elements
            result.Words[i] = ~Words[i];

        return result;
    }

    public BitArrayL32 Or(BitArrayL32 other, BitArrayL32 result)
    {
        if (Count != other.Count)
            ThrowHelper.ThrowArgumentException(nameof(other), $"{nameof(BitArrayL32)} dimension mismatch. This {nameof(BitArrayL32)} has a bit count of {Count}, while {nameof(other)} has a bit count of {result.Count}.");
        if (Count != result.Count)
            ThrowHelper.ThrowArgumentException(nameof(result), $"{nameof(BitArrayL32)} dimension mismatch. This {nameof(BitArrayL32)} has a bit count of {Count}, while {nameof(result)} has a bit count of {result.Count}.");
        if (Words.Length == 0)
            return result;

        int i = 0;
        for (; i < Words.Length - Vector<TWord>.Count; i += Vector<TWord>.Count) // SIMD loop
            (new Vector<TWord>(Words[i..]) | new Vector<TWord>(other.Words[i..])).CopyTo(result.Words[i..]);

        for (; i < Words.Length; i++) // SISD loop, tail elements
            result.Words[i] = Words[i] | other.Words[i];

        return result;
    }

    public BitArrayL32 And(BitArrayL32 other, BitArrayL32 result)
    {
        if (Count != other.Count)
            ThrowHelper.ThrowArgumentException(nameof(other), $"{nameof(BitArrayL32)} dimension mismatch. This {nameof(BitArrayL32)} has a bit count of {Count}, while {nameof(other)} has a bit count of {result.Count}.");
        if (Count != result.Count)
            ThrowHelper.ThrowArgumentException(nameof(result), $"{nameof(BitArrayL32)} dimension mismatch. This {nameof(BitArrayL32)} has a bit count of {Count}, while {nameof(result)} has a bit count of {result.Count}.");
        if (Words.Length == 0)
            return result;

        int i = 0;
        for (; i < Words.Length - Vector<TWord>.Count; i += Vector<TWord>.Count) // SIMD loop
            (new Vector<TWord>(Words[i..]) & new Vector<TWord>(other.Words[i..])).CopyTo(result.Words[i..]);

        for (; i < Words.Length; i++) // SISD loop, tail elements
            result.Words[i] = Words[i] & other.Words[i];


        return result;
    }

    public BitArrayL32 Xor(BitArrayL32 other, BitArrayL32 result)
    {
        if (Count != other.Count)
            ThrowHelper.ThrowArgumentException(nameof(other), $"{nameof(BitArrayL32)} dimension mismatch. This {nameof(BitArrayL32)} has a bit count of {Count}, while {nameof(other)} has a bit count of {result.Count}.");
        if (Count != result.Count)
            ThrowHelper.ThrowArgumentException(nameof(result), $"{nameof(BitArrayL32)} dimension mismatch. This {nameof(BitArrayL32)} has a bit count of {Count}, while {nameof(result)} has a bit count of {result.Count}.");
        if (Words.Length == 0)
            return result;

        int i = 0;
        for (; i < Words.Length - Vector<TWord>.Count; i += Vector<TWord>.Count) // SIMD loop
            (new Vector<TWord>(Words[i..]) ^ new Vector<TWord>(other.Words[i..])).CopyTo(result.Words[i..]);

        for (; i < Words.Length; i++) // SISD loop, tail elements
            result.Words[i] = Words[i] ^ other.Words[i];


        return result;
    }


    public bool Any()
    {
        int i = 0;
        for (; i < Words.Length - Vector<TWord>.Count; i += Vector<TWord>.Count) // SIMD loop
            if (!new Vector<TWord>(Words[i..]).Equals(Vector<TWord>.Zero))
                return true;

        for (; i < Words.Length; i++)
            if (Words[i] != ZERO)
                return true;

        return false;
    }

    public bool All()
    {
        int i = 0;
        Vector<TWord> vAllOnes = ~Vector<TWord>.Zero;
        for (; i < Words.Length - Vector<TWord>.Count; i += Vector<TWord>.Count) // SIMD loop
            if (!new Vector<TWord>(Words[i..]).Equals(vAllOnes))
                return false;

        for (; i < Words.Length; i++)
            if (Words[i] != ALL_BITS)
                return false;

        return true;
    }


    public TInd PopCount()
    {
        TInd count = 0;
        for (int i = 0; i < Words.Length; i++)
            count += BitOperations.PopCount(Words[i]);

        return count;
    }

    public bool Parity()
    {
        TWord c = 0;
        int i = 0;
        if (Words.Length >= 2 * Vector<TWord>.Count)
        {
            Vector<TWord> parityBytes = new Vector<TWord>(Words);
            i = Vector<TWord>.Count;
            for (; i < Words.Length - Vector<TWord>.Count; i += Vector<TWord>.Count) // SIMD loop
                parityBytes ^= new Vector<TWord>(Words[i..]);

            for (int k = 0; k < Vector<TWord>.Count; k++)
                c ^= parityBytes[k];
        }

        for (; i < Words.Length; i++) // SISD loop, tail elements
            c ^= Words[i];

        return (BitOperations.PopCount(c) & 1) != 0;
    }

    public bool Equals(BitArrayL32 other)
    {
        if (Count != other.Count)
            return false;

        int i = 0;
        for (; i < Words.Length - Vector<TWord>.Count; i++)
            if (!new Vector<TWord>(Words[i..]).Equals(new Vector<TWord>(other.Words[i..])))
                return false;

        for (; i < Words.Length; i++)
            if (Words[i] != other.Words[i])
                return false;

        return true;
    }
    public override bool Equals(object? obj) => obj is BitArrayL32 barr && Equals(barr);

    public override int GetHashCode() => HashCode.Combine(Words);

    IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();

    IEnumerator<bool> IEnumerable<bool>.GetEnumerator()
    {
        for (int i = 0; i < Words.Length; i++)
        {
            for (TWord p = ONE; p < MAX_BIT; p <<= 1)
                yield return (Words[i] & p) != 0;
            yield return (Words[i] & MAX_BIT) != 0;
        }
    }

    IEnumerator<TInd> IEnumerable<TInd>.GetEnumerator()
    {
        TInd count = 0;
        for (int i = 0; i < Words.Length; i++)
        {
            if (Words[i] == 0)
            {
                count += BITS_PER_WORD;
                continue;
            }

            for (TWord p = ONE; p < MAX_BIT; p <<= 1)
            {
                if ((Words[i] & p) != 0)
                    yield return count;
                count++;
            }
            if ((Words[i] & MAX_BIT) != 0)
                yield return count;
            count++;
        }
    }

    public void CopyTo(TInd[] array, int arrayIndex)
    {
        int count = arrayIndex;
        foreach (TInd item in this as IEnumerable<TInd>)
            array[count++] = item;
    }


    #region operator overloads
    public static BitArrayL32 operator ~(BitArrayL32 a) => a.Not();
    public static BitArrayL32 operator |(BitArrayL32 a, BitArrayL32 b) => a.Or(b);
    public static BitArrayL32 operator &(BitArrayL32 a, BitArrayL32 b) => a.And(b);
    public static BitArrayL32 operator ^(BitArrayL32 a, BitArrayL32 b) => a.Xor(b);
    public static bool operator ==(BitArrayL32 a, BitArrayL32 b) => a.Equals(b);
    public static bool operator !=(BitArrayL32 a, BitArrayL32 b) => !a.Equals(b);
    #endregion operator overloads

    public BitArrayL32 CreateSimilarEmpty() => new BitArrayL32(Count);
    public BitArrayL32 CreateEmpty(int count) => new BitArrayL32(count);
    public BitArrayL32 Create(TWord[] arr) => new BitArrayL32(arr);


    public override string ToString()
    {
        char[] chars = new char[Count];
        for (int i = 0; i < Count; i++)
            chars[i] = this[i] ? '1' : '0';

        return new string(chars);
    }


    /// <summary>
    /// An empty <see cref="BitArrayL32"/>. Use this object instead of creating new empty <see cref="BitArrayL32"/>s to save memory.
    /// </summary>
    public static readonly BitArrayL32 Empty = new BitArrayL32(Array.Empty<TWord>());
}
