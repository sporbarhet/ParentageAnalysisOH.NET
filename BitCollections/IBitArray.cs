namespace Sporbarhet.Parentage.BitCollections;


/// <summary>
/// An array of bits with indexes of type <typeparamref name="TInd"/> organized contiguously in memory with words of type <see cref="TWord"/>.
/// </summary>
/// <typeparam name="TInd">The type of indexes.</typeparam>
/// <typeparam name="TWord">The type of the words, used to organize the bits in memory.</typeparam>
/// <typeparam name="TSelf">The type of the implementing class/struct.</typeparam>
public interface IBitArray<TInd, TWord, TSelf> : IBitSet<TInd, TSelf>
    where TSelf : IBitArray<TInd, TWord, TSelf> where TInd : notnull
{
    new bool this[TInd index] { get; set; }

    TWord[] Words { get; }

    TSelf CreateSimilarEmpty();
    TSelf CreateEmpty(TInd count);
    TSelf Create(TWord[] arr);
}
