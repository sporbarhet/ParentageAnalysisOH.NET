using System.Diagnostics;

namespace Sporbarhet.Parentage.Extensions;

/// <summary>
/// Extension methods for <see cref="Stopwatch"/>es.
/// </summary>

public static class StopwatchExtensions
{
    /// <summary>
    /// The number of elapsed seconds of this <see cref="Stopwatch"/>.
    /// </summary>
    /// <param name="sw">A <see cref="Stopwatch"/>.</param>
    /// <returns>The elapsed time of this <see cref="Stopwatch"/> in seconds.</returns>
    public static double ElapsedSeconds(this Stopwatch sw) => Math.Round(sw.ElapsedMilliseconds * 0.001, 3);
}
