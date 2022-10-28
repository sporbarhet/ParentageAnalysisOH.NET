namespace Sporbarhet.Parentage.BitCollections;


/// <summary>
/// An array of bits with indexes of type <typeparamref name="TInd"/> organized contiguously in memory with words of type <typeparamref name="TWord"/>.
/// </summary>
/// <typeparam name="TInd">The type of indexes.</typeparam>
/// <typeparam name="TWord">The type of the words, used to organize the bits in memory.</typeparam>
/// <typeparam name="TSelf">The type of the implementing class/struct.</typeparam>
public interface IBitArray<TInd, TWord, TSelf> : IBitSet<TInd, TSelf>
    where TSelf : IBitArray<TInd, TWord, TSelf> where TInd : notnull
{
    /// <summary>
    /// The bits of this <see cref="IBitArray{TInd, TWord, TSelf}"/>.
    /// </summary>
    /// <param name="index">The index of the bit.</param>
    /// <returns>The value of the bit at <paramref name="index"/>.</returns>
    new bool this[TInd index] { get; set; }

    /// <summary>
    /// The array of words in this <see cref="IBitArray{TInd, TWord, TSelf}"/>.
    /// </summary>
    TWord[] Words { get; }


    /// <summary>
    /// Create a similar bit array, i.e. with the same number of words, and all bits set to <c>0</c>.
    /// </summary>
    /// <returns>A similar and empty bit array.</returns>
    TSelf CreateSimilarEmpty();

    /// <summary>
    /// Create an empty bit array, i.e. a bit array with all bits set to <c>0</c>, and a capacity for at least <paramref name="count"/> bits.
    /// </summary>
    /// <param name="count">The minimum bit capacity of the new bit array.</param>
    /// <returns>An empty bit array with a capacity for at least <paramref name="count"/> bits.</returns>
    TSelf CreateEmpty(TInd count);
    /// <summary>
    /// Create a bit array from a words array. It will use the supplied array in place.
    /// </summary>
    /// <param name="arr">The words array to use.</param>
    /// <returns>A bit array using the supplied words array.</returns>
    TSelf Create(TWord[] arr);
}
