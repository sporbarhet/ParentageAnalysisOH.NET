namespace Sporbarhet.Parentage.BitCollections.Generic;

//
// Summary:
//     Represents a strongly-typed, read-only sized class.
//
// Type parameters:
//   I:
//     The type of the size.
public interface ISized<out I> where I : notnull
{
    //
    // Summary:
    //     Gets the number of elements in the class.
    //
    // Returns:
    //     The number of elements in the class.
    I Count { get; }
}
