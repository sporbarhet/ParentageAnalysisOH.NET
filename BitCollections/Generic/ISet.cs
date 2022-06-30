namespace Sporbarhet.Parentage.BitCollections.Generic;

//
// Summary:
//     Provides the base interface for the abstraction of sets.
//
// Type parameters:
//   T:
//     The type of elements in the set.
public interface ISet<T, out I> : ICollection<T, I> where I : notnull
{
    //
    // Summary:
    //     Adds an element to the current set and returns a value to indicate if the element
    //     was successfully added.
    //
    // Parameters:
    //   item:
    //     The element to add to the set.
    //
    // Returns:
    //     true if the element is added to the set; false if the element is already in the
    //     set.
    bool TryAdd(T item);

    //
    // Summary:
    //     Removes all elements in the specified collection from the current set.
    //
    // Parameters:
    //   other:
    //     The collection of items to remove from the set.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     other is null.
    void ExceptWith(IEnumerable<T> other);

    //
    // Summary:
    //     Modifies the current set so that it contains only elements that are also in a
    //     specified collection.
    //
    // Parameters:
    //   other:
    //     The collection to compare to the current set.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     other is null.
    void IntersectWith(IEnumerable<T> other);

    //
    // Summary:
    //     Determines whether the current set is a proper (strict) subset of a specified
    //     collection.
    //
    // Parameters:
    //   other:
    //     The collection to compare to the current set.
    //
    // Returns:
    //     true if the current set is a proper subset of other; otherwise, false.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     other is null.
    bool IsProperSubsetOf(IEnumerable<T> other);

    //
    // Summary:
    //     Determines whether the current set is a proper (strict) superset of a specified
    //     collection.
    //
    // Parameters:
    //   other:
    //     The collection to compare to the current set.
    //
    // Returns:
    //     true if the current set is a proper superset of other; otherwise, false.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     other is null.
    bool IsProperSupersetOf(IEnumerable<T> other);

    //
    // Summary:
    //     Determines whether a set is a subset of a specified collection.
    //
    // Parameters:
    //   other:
    //     The collection to compare to the current set.
    //
    // Returns:
    //     true if the current set is a subset of other; otherwise, false.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     other is null.
    bool IsSubsetOf(IEnumerable<T> other);

    //
    // Summary:
    //     Determines whether the current set is a superset of a specified collection.
    //
    // Parameters:
    //   other:
    //     The collection to compare to the current set.
    //
    // Returns:
    //     true if the current set is a superset of other; otherwise, false.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     other is null.
    bool IsSupersetOf(IEnumerable<T> other);

    //
    // Summary:
    //     Determines whether the current set overlaps with the specified collection.
    //
    // Parameters:
    //   other:
    //     The collection to compare to the current set.
    //
    // Returns:
    //     true if the current set and other share at least one common element; otherwise,
    //     false.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     other is null.
    bool Overlaps(IEnumerable<T> other);

    //
    // Summary:
    //     Determines whether the current set and the specified collection contain the same
    //     elements.
    //
    // Parameters:
    //   other:
    //     The collection to compare to the current set.
    //
    // Returns:
    //     true if the current set is equal to other; otherwise, false.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     other is null.
    bool SetEquals(IEnumerable<T> other);

    //
    // Summary:
    //     Modifies the current set so that it contains only elements that are present either
    //     in the current set or in the specified collection, but not both.
    //
    // Parameters:
    //   other:
    //     The collection to compare to the current set.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     other is null.
    void SymmetricExceptWith(IEnumerable<T> other);

    //
    // Summary:
    //     Modifies the current set so that it contains all elements that are present in
    //     the current set, in the specified collection, or in both.
    //
    // Parameters:
    //   other:
    //     The collection to compare to the current set.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     other is null.
    void UnionWith(IEnumerable<T> other);
}
