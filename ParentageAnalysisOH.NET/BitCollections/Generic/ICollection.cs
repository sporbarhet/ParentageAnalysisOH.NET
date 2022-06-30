namespace Sporbarhet.Parentage.BitCollections.Generic;
//
// Summary:
//     Defines methods to manipulate generic collections.
//
// Type parameters:
//   T:
//     The type of the elements in the collection.
public interface ICollection<T, out I> : ISized<I> where I : notnull
{
    //
    // Summary:
    //     Gets a value indicating whether the ICollection
    //     is read-only.
    //
    // Returns:
    //     true if the ICollection is read-only; otherwise,
    //     false.
    bool IsReadOnly { get; }

    //
    // Summary:
    //     Adds an item to the ICollection.
    //
    // Parameters:
    //   item:
    //     The object to add to the ICollection.
    //
    // Exceptions:
    //   T:System.NotSupportedException:
    //     The ICollection is read-only.
    void Add(T item);

    //
    // Summary:
    //     Removes all items from the ICollection.
    //
    // Exceptions:
    //   T:System.NotSupportedException:
    //     The ICollection is read-only.
    void Clear();

    //
    // Summary:
    //     Determines whether the ICollection contains a specific
    //     value.
    //
    // Parameters:
    //   item:
    //     The object to locate in the ICollection.
    //
    // Returns:
    //     true if item is found in the ICollection; otherwise,
    //     false.
    bool Contains(T item);

    //
    // Summary:
    //     Removes the first occurrence of a specific object from the ICollection.
    //
    // Parameters:
    //   item:
    //     The object to remove from the ICollection.
    //
    // Exceptions:
    //   T:System.NotSupportedException:
    //     The ICollection is read-only.
    void Remove(T item);

    //
    // Summary:
    //     Removes the first occurrence of a specific object from the ICollection.
    //
    // Parameters:
    //   item:
    //     The object to remove from the ICollection.
    //
    // Returns:
    //     true if item was successfully removed from the ICollection;
    //     otherwise, false. This method also returns false if item is not found in the
    //     original ICollection.
    //
    // Exceptions:
    //   T:System.NotSupportedException:
    //     The ICollection is read-only.
    bool TryRemove(T item);
}
