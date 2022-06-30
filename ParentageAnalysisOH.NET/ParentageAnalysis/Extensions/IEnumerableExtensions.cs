namespace Sporbarhet.Parentage.Extensions;

/// <summary>
/// A collection of general extension methods relating to enumeration.
/// </summary>
public static class IEnumerableExtensions
{
    /// <summary>
    /// Creates an array from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <remarks>
    /// Avoids reallocations by taking the final <paramref name="capacity"/> as an argument.
    /// </remarks>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="e">An <see cref="IEnumerable{T}"/> to create an array from.</param>
    /// <param name="capacity">The final capacity of the produced array.</param>
    /// <returns>An array that contains the elements from the input sequence.</returns>
    public static T[] ToArray<T>(this IEnumerable<T> e, int capacity)
    {
        T[] array = new T[capacity];
        int idx = 0;
        foreach (T item in e)
            array[idx++] = item;

        return array;
    }

    /// <summary>
    /// Creates an array from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <remarks>
    /// Avoids reallocations by taking the capacity as an argument.
    /// </remarks>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="e">An <see cref="IEnumerable{T}"/> to create an array from.</param>
    /// <param name="capacity">The final capacity of the produced array.</param>
    /// <returns>An array that contains the elements from the input sequence.</returns>
    public static T[] ToArray<T>(this IEnumerable<T> e, long capacity)
    {
        T[] array = new T[capacity];
        long idx = 0;
        foreach (T item in e)
            array[idx++] = item;

        return array;
    }


    /// <summary>
    /// Creates a list from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <remarks>
    /// Avoids needless reallocations by accepting an initial capacity as an argument.
    /// </remarks>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="e">An <see cref="IEnumerable{T}"/> to create a list from.</param>
    /// <param name="initialCapacity">The initial capacity of the produced list.</param>
    /// <returns>A list that contains the elements from the input sequence.</returns>
    public static List<T> ToList<T>(this IEnumerable<T> e, int initialCapacity)
    {
        var list = new List<T>(initialCapacity);
        list.AddRange(e);
        return list;
    }


    /// <summary>
    /// Applies an action for each item in an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="e">An <see cref="IEnumerable{T}"/> to perform an action on.</param>
    /// <param name="action">The action to perform on each item in the <see cref="IEnumerable{T}"/>.</param>
    public static void ForEach<T>(this IEnumerable<T> e, Action<T> action)
    {
        foreach (T item in e)
            action(item);
    }

    /// <summary>
    /// Enumerates a two-dimensional array.
    /// </summary>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="m">The two-dimensional array to enumerate.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of the items in the two-dimensional array.</returns>
    public static IEnumerable<T> Enumerate<T>(this T[,] m)
    {
        int len0 = m.GetLength(0), len1 = m.GetLength(1);
        for (int i = 0; i < len0; i++)
            for (int j = 0; j < len1; j++)
                yield return m[i, j];
    }
}
