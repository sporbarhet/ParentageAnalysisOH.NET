namespace Sporbarhet.Parentage.BitCollections.Extensions;

/// <summary>
/// A collection of extension methods for <see cref="IBitArray{TInd, TWord, TSelf}"/>s.
/// </summary>
public static class BitArrayExtensions
{
    /// <inheritdoc cref="IBitSet{TInd, TWord, TSelf}.Not(TSelf)"/>
    public static TSelf Not<TInd, TArr, TSelf>(this IBitArray<TInd, TArr, TSelf> a)
        where TSelf : IBitArray<TInd, TArr, TSelf> where TInd : notnull
        => a.Not(a.CreateSimilarEmpty());

    /// <inheritdoc cref="IBitSet{TInd, TWord, TSelf}.Or(TSelf, TSelf)"/>
    public static TSelf Or<TInd, TArr, TSelf>(this IBitArray<TInd, TArr, TSelf> a, TSelf b)
        where TSelf : IBitArray<TInd, TArr, TSelf> where TInd : notnull
    {
        if (b is null)
            throw new ArgumentNullException(nameof(b));
        if (!a.Count.Equals(b.Count))
            throw new ArgumentException($"Array bit count mismatch! This array's count is {a.Count} while other's is {b.Count}.", nameof(b));

        return a.Or(b, a.CreateSimilarEmpty());
    }

    /// <inheritdoc cref="IBitSet{TInd, TWord, TSelf}.And(TSelf, TSelf)"/>
    public static TSelf And<TInd, TArr, TSelf>(this IBitArray<TInd, TArr, TSelf> a, TSelf b)
        where TSelf : IBitArray<TInd, TArr, TSelf> where TInd : notnull
    {
        if (b is null)
            throw new ArgumentNullException(nameof(b));
        if (!a.Count.Equals(b.Count))
            throw new ArgumentException($"Array bit count mismatch! This array's count is {a.Count} while other's is {b.Count}.", nameof(b));

        return a.And(b, a.CreateSimilarEmpty());
    }

    /// <inheritdoc cref="IBitSet{TInd, TWord, TSelf}.Xor(TSelf, TSelf)"/>
    public static TSelf Xor<TInd, TArr, TSelf>(this IBitArray<TInd, TArr, TSelf> a, TSelf b)
        where TSelf : IBitArray<TInd, TArr, TSelf> where TInd : notnull
    {
        if (b is null)
            throw new ArgumentNullException(nameof(b));
        if (!a.Count.Equals(b.Count))
            throw new ArgumentException($"Array bit count mismatch! This array's count is {a.Count} while other's is {b.Count}.", nameof(b));

        return a.Xor(b, a.CreateSimilarEmpty());
    }
}
