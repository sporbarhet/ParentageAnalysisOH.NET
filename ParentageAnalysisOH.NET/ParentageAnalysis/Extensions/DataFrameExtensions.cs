using Microsoft.Data.Analysis;

namespace Sporbarhet.Parentage.Extensions;

/// <summary>
/// A collection of general extension methods relating to DataFrames.
/// </summary>
public static class DataFrameExtensions
{
    /// <summary>
    /// Calculates the standard deviation of <paramref name="column"/>.
    /// </summary>
    /// <remarks>
    /// Assumes inferred mean.
    /// </remarks>
    /// <param name="column"></param>
    /// <param name="inferredMean"></param>
    /// <returns></returns>
    public static double StdDev(this DataFrameColumn column, double inferredMean = double.NaN)
    {
        if (double.IsNaN(inferredMean))
            inferredMean = column.Mean();

        var devFromMean = column - inferredMean;
        double sum = (double)(devFromMean * devFromMean).Sum();
        return Math.Sqrt(sum / (column.Length - 1)); // One less degree of freedom since the mean is inferred.
    }
}
