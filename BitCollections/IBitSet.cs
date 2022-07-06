using Sporbarhet.Parentage.BitCollections.Generic;

namespace Sporbarhet.Parentage.BitCollections;

/// <summary>
/// An set of bits with indexes of type <typeparamref name="TInd"/>.
/// </summary>
/// <typeparam name="TInd">The type of indexes.</typeparam>
/// <typeparam name="TSelf">The type of the implementing class/struct.</typeparam>
public interface IBitSet<TInd, TSelf> : ICollection<TInd, TInd>, IReadOnlyList<bool, TInd>, IEquatable<TSelf>
    where TSelf : IBitSet<TInd, TSelf> where TInd : notnull
{
    /// <summary>
    /// Sets all bits in this bit array to <paramref name="value"/>.
    /// <example>
    /// For instance, the method call <c>SetAll(false)</c> has the same effect as <c>Clear()</c>.
    /// </example>
    /// </summary>
    /// <param name="value">The value to set all bits of this <see cref="IBitSet{TInd, TSelf}"/> to.</param>
    void SetAll(bool value);

    /// <summary>
    /// Nots this <see cref="IBitSet{TInd, TSelf}"/> and places the result in <paramref name="result"/>.
    /// This can also be understood as taking the complement of the bit set.
    /// </summary>
    /// <param name="result">The bit set to set the result in.</param>
    /// <returns>The complement of this bit set.</returns>
    TSelf Not(TSelf result);

    /// <summary>
    /// Performs a logical or operation between this <see cref="IBitSet{TInd, TSelf}"/> and <paramref name="other"/>, and places the result in <paramref name="result"/>.
    /// This can also be understood as taking the union of the two bit sets.
    /// </summary>
    /// <param name="other">The other/second <see cref="IBitSet{TInd, TSelf}"/> to perform this operation on.</param>
    /// <param name="result">The bit set to set the result in.</param>
    /// <returns>The union of this <see cref="IBitSet{TInd, TSelf}"/> and <paramref name="other"/>.</returns>
    TSelf Or(TSelf other, TSelf result);

    /// <summary>
    /// Performs a logical and operation between this <see cref="IBitSet{TInd, TSelf}"/> and <paramref name="other"/>, and places the result in <paramref name="result"/>.
    /// This can also be understood as taking the intersection of the two bit sets.
    /// </summary>
    /// <param name="other">The other/second <see cref="IBitSet{TInd, TSelf}"/> to perform this operation on.</param>
    /// <param name="result">The bit set to set the result in.</param>
    /// <returns>The intersection of this <see cref="IBitSet{TInd, TSelf}"/> and <paramref name="other"/>.</returns>
    TSelf And(TSelf other, TSelf result);

    /// <summary>
    /// Performs a logical exclusive or operation between this <see cref="IBitSet{TInd, TSelf}"/> and <paramref name="other"/>, and places the result in <paramref name="result"/>.
    /// This can also be understood as taking the symmetric difference of the two bit sets.
    /// </summary>
    /// <param name="other">The other/second <see cref="IBitSet{TInd, TSelf}"/> to perform this operation on.</param>
    /// <param name="result">The bit set to set the result in.</param>
    /// <returns>The symmetric difference of this <see cref="IBitSet{TInd, TSelf}"/> and <paramref name="other"/>.</returns>
    TSelf Xor(TSelf other, TSelf result);

    /// <summary>
    /// Whether any bits in this <see cref="IBitSet{TInd, TSelf}"/> are set.
    /// </summary>
    /// <returns>Returns <see langword="true"/> if any bits are set, otherwise <see langword="false"/>.</returns>
    bool Any();

    /// <summary>
    /// Whether all bits in this <see cref="IBitSet{TInd, TSelf}"/> are set.
    /// </summary>
    /// <returns>Returns <see langword="true"/> if all bits are set, otherwise <see langword="false"/>.</returns>
    bool All();

    /// <summary>
    /// Calculates the parity of bits in this <see cref="IBitSet{TInd, TSelf}"/>.
    /// The parity is a <see cref="bool"/> value denoting whether an odd number of bits are set or not.
    /// </summary>
    /// <returns>Returns <see langword="true"/> if an odd number of bits are set, <see langword="false"/> if an even number of bits are set.</returns>
    bool Parity();

    /// <summary>
    /// Calculates the population count of this <see cref="IBitSet{TInd, TSelf}"/>. Meaning the number of bits set to <see langword="true"/>.
    /// </summary>
    /// <returns>The population count of this <see cref="IBitSet{TInd, TSelf}"/>.</returns>
    TInd PopCount();
}
