namespace Sporbarhet.Parentage.BitCollections.Generic;

//
// Summary:
//     Represents a read-only collection of elements that can be accessed by index.
//
// Type parameters:
//   T:
//     The type of elements in the read-only list.
//   I:
//     The type of indices in the read-only list.
public interface IReadOnlyList<out T, I> : ISized<I> where I : notnull
{
    //
    // Summary:
    //     Gets the element at the specified index in the read-only list.
    //
    // Parameters:
    //   index:
    //     The zero-based index of the element to get.
    //
    // Returns:
    //     The element at the specified index in the read-only list.
    T this[I index] { get; }
}
