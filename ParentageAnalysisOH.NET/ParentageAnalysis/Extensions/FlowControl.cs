using System.Globalization;

namespace Sporbarhet.Parentage.Extensions;

/// <summary>
/// A collection of helper methods for common flow control patterns.
/// </summary>
public static class FlowControl
{
    /// <summary>
    /// Retries running <paramref name="f"/> and returning its result, catching exceptions of type <typeparamref name="TException"/>
    /// until <paramref name="f"/> is successfully completed or the number of retries exceeds <paramref name="retries"/>.
    /// Uses a backoff strategy specified by <paramref name="backoffMs"/>, starting with a base wait time of <paramref name="baseRetrySleepMs"/> ms.
    /// The default backoff strategy is to double sleep duration for every failed attempt.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TException"></typeparam>
    /// <param name="f"></param>
    /// <param name="retries">Maximal number of retries before giving up and throwing the caught exception.</param>
    /// <param name="baseRetrySleepMs">Base sleep between retries in milliseconds.</param>
    /// <param name="backoffMs">The backoff strategy. It takes in the current sleep duration in milliseconds and returns the next sleep duration in milliseconds. Defaults to a doubling strategy if <see cref="null"/>.</param>
    /// <returns></returns>
    public static TResult WithRetries<TResult, TException>(Func<TResult> f, int retries = 1, int baseRetrySleepMs = 50, Func<int, int>? backoffMs = null)
        where TException : Exception
    {
        if (backoffMs is null)
            backoffMs = t => t * 2;

        while (true)
            try
            {
                return f();
            }
            catch (TException e)
            {
                if (--retries <= 0)
                    throw e;
                Thread.Sleep(baseRetrySleepMs);
                baseRetrySleepMs = backoffMs(baseRetrySleepMs);
            }
    }

    /// <summary>
    /// Calls the function <paramref name="f"/> in an environment where the culture is set to <paramref name="culture"/>.
    /// This method should also work if <paramref name="f"/> spawns new threads during its lifetime, as long as they have terminated by the time <paramref name="f"/> returns.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of <paramref name="f"/>.</typeparam>
    /// <param name="culture">The culture to set the environment to.</param>
    /// <param name="f">The function to call.</param>
    /// <returns>The result from the function call made in the specified <paramref name="culture"/>.</returns>
    public static TResult WithCulture<TResult>(CultureInfo culture, Func<TResult> f)
    {
        var oldCulture = CultureInfo.CurrentCulture;
        var oldDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;

            return f();
        }
        finally
        {
            CultureInfo.CurrentCulture = oldCulture;
            CultureInfo.DefaultThreadCurrentCulture = oldDefaultCulture;
        }
    }
}
