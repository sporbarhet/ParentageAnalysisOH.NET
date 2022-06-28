namespace Sporbarhet.Parentage.BitCollections.Extensions;
/// <summary>
/// A collection of array extension methods to make their interface more similar to <see cref="Span{T}"/>.
/// </summary>
static class ArrayExtensions
{
    /// <summary>
    /// Assigns the given value of type T to each element of the specified array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    /// <param name="array">The array to be filled.</param>
    /// <param name="value">The value to assign to each array element.</param>
    public static void Fill<T>(this T[] array, T value) => Array.Fill(array, value);

    /// <summary>
    /// Clears the contents of an <paramref name="array"/>.
    /// </summary>
    /// <param name="array">The array to be cleared.</param>
    public static void Clear(this Array array) => Array.Clear(array);
}
