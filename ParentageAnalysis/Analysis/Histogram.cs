using Microsoft.Toolkit.Diagnostics;
using Sporbarhet.Parentage.Extensions;

namespace Sporbarhet.Parentage.Analysis;

/// <summary>
/// A class for creating and drawing text histograms.
/// </summary>
/// <typeparam name="T">The numeric type of the X-axis. Must be one of the types
/// <see cref="byte"/>, <see cref="sbyte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>, <see cref="Half"/> or <see cref="char"/>.
/// </typeparam>
public class Histogram<T> where T : unmanaged
{
    /// <summary>
    /// The buckets of the histogram. Contains the counts for each bar on the plot.
    /// The first and last position of the array are reserved for values below <see cref="XMin"/> and above or equal to <see cref="XMax"/> respectively.
    /// </summary>
    /// <remarks>
    /// The minimum value in the <c>i</c>'th bucket is <c>XStep * i + XMin</c>.
    /// </remarks>
    public readonly ulong[] Buckets;
    /// <summary>
    /// The minimum value in the histogram.
    /// </summary>
    public double XMin { get; protected set; }
    /// <summary>
    /// The maximum value in the histogram.
    /// </summary>
    public double XMax { get; protected set; }
    /// <summary>
    /// The step width between buckets in the histogram.
    /// </summary>
    public double XStep { get; protected set; }

    /// <summary>
    /// The value offset for computing the bucket index of a value.
    /// See also <see cref="GetBucketIndex(T)"/>.
    /// </summary>
    /// <remarks>
    /// The index of <c>x</c> is computed as <c>Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1)</c>.
    /// </remarks>
    protected double XOff { get; set; }
    /// <summary>
    /// The value coefficient for computing the bucket index of a value.
    /// See also <see cref="GetBucketIndex(T)"/>.
    /// </summary>
    /// <remarks>
    /// The index of the value <c>x</c> is computed as <c>Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1)</c>.
    /// </remarks>
    protected double XCoeff { get; set; }

    /// <summary>
    /// Type generic method for getting the bucket index that the value <paramref name="t"/> belongs to.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public int GetBucketIndex(T t) => t switch
    {
        byte x => GetBucketIndex(x),
        sbyte x => GetBucketIndex(x),
        short x => GetBucketIndex(x),
        ushort x => GetBucketIndex(x),
        int x => GetBucketIndex(x),
        uint x => GetBucketIndex(x),
        long x => GetBucketIndex(x),
        ulong x => GetBucketIndex(x),
        float x => GetBucketIndex(x),
        double x => GetBucketIndex(x),
        decimal x => GetBucketIndex(x),
        Half x => GetBucketIndex(x),
        char x => GetBucketIndex(x),
        _ => ThrowHelper.ThrowArgumentException<int>(nameof(t), $"Argument of invalid type was given. Argument has type {typeof(T)}, but the only supported types are {nameof(Byte)}, {nameof(SByte)}, {nameof(Int16)}, {nameof(UInt16)}, {nameof(Int32)}, {nameof(UInt32)}, {nameof(Int64)}, {nameof(UInt64)}, {nameof(Single)}, {nameof(Double)}, {nameof(Decimal)}, {nameof(Half)}, and {nameof(Char)}."),
    };

    public int GetBucketIndex(byte x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(sbyte x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(short x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(ushort x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(int x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(uint x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(long x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(ulong x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(float x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(double x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(decimal x) => Math.Clamp((int)(XCoeff * (double)x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(Half x) => Math.Clamp((int)(XCoeff * (double)x + XOff), 0, Buckets.Length - 1);
    public int GetBucketIndex(char x) => Math.Clamp((int)(XCoeff * x + XOff), 0, Buckets.Length - 1);


    protected Histogram(int count, double xMin, double xMax)
    {
        Buckets = new ulong[count + 2];
        XMin = xMin;
        XMax = xMax;
        XStep = (XMax - XMin) / count;
        XOff = 1d - XMin / XStep;
        XCoeff = 1d / XStep;
    }

    /// <summary>
    /// Histograms the <paramref name="values"/> into <paramref name="count"/> buckets.
    /// If <paramref name="xMin"/> or <paramref name="xMax"/> is specified, these will be used to determine how the values place in relation to the buckets.
    /// It also determines how the x axis of the histogram will look in a plot.
    /// </summary>
    /// <param name="values">The values to histogram.</param>
    /// <param name="count">The number of buckets.</param>
    /// <param name="xMin">The minimum value to include in the histogram.</param>
    /// <param name="xMax">The maximum value to include in the histogram.</param>
    public Histogram(IEnumerable<T> values, int count, double xMin = double.NaN, double xMax = double.NaN)
        : this(count, double.IsNaN(xMin) ? Convert.ToDouble(values.Min()) : xMin, double.IsNaN(xMax) ? Convert.ToDouble(values.Max()) * (1 + 1e-12) : xMax)
    {
        //PERF: for very large datasets, this may be a heavy operation and an optimization may be called for.
        //See for instance https://stackoverflow.com/questions/12985949/methods-to-vectorise-histogram-in-simd
        foreach (T v in values)
            Buckets[GetBucketIndex(v)]++;
    }

    /// <summary>
    /// Plots this histogram with a width of <paramref name="width"/> and using the provided <paramref name="scale"/>.
    /// See also <seealso cref="PlotString(int, Func{double, double}, Func{double, double}, double)"/>.
    /// </summary>
    /// <param name="width">The maximal length of a bar in the diagram in number of characters. The atual width of the diagram will be slightly wider, to make room for the axes and ticks.</param>
    /// <param name="scale">The scaling mode of the y axis.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown if an unrecognized <see cref="AxisScale"/> is given.</exception>
    public string PlotString(int width, AxisScale scale = AxisScale.Linear, int xDecimalPlaces = 0, int xMinTickWidth = 0, bool trimXaxis = false) => scale switch
    {
        AxisScale.Linear => PlotString(width, x => x, x => x, xDecimalPlaces, xMinTickWidth, trimXaxis: trimXaxis),
        AxisScale.Quadratic => PlotString(width, x => x * x, Math.Sqrt, xDecimalPlaces, xMinTickWidth, trimXaxis: trimXaxis),
        AxisScale.Sqrt => PlotString(width, Math.Sqrt, x => x * x, xDecimalPlaces, xMinTickWidth, trimXaxis: trimXaxis),
        AxisScale.Cubic => PlotString(width, x => x * x * x, Math.Cbrt, xDecimalPlaces, xMinTickWidth, trimXaxis: trimXaxis),
        AxisScale.Cbrt => PlotString(width, Math.Cbrt, x => x * x * x, xDecimalPlaces, xMinTickWidth, trimXaxis: trimXaxis),
        AxisScale.Exp => PlotString(width, Math.Exp, Math.Log, xDecimalPlaces, xMinTickWidth, 1, trimXaxis: trimXaxis),
        AxisScale.Log => PlotString(width, Math.Log, Math.Exp, xDecimalPlaces, xMinTickWidth, trimXaxis: trimXaxis),
        AxisScale.Exp10 => PlotString(width, x => Math.Pow(10, x), Math.Log10, xDecimalPlaces, xMinTickWidth, 1, trimXaxis: trimXaxis),
        AxisScale.Log10 => PlotString(width, Math.Log10, x => Math.Pow(10, x), xDecimalPlaces, xMinTickWidth, trimXaxis: trimXaxis),
        AxisScale.Exp2 => PlotString(width, x => Math.Pow(2, x), Math.Log2, xDecimalPlaces, xMinTickWidth, 1, trimXaxis: trimXaxis),
        AxisScale.Log2 => PlotString(width, Math.Log2, x => Math.Pow(2, x), xDecimalPlaces, xMinTickWidth, trimXaxis: trimXaxis),
        _ => throw new ArgumentException($"Axis scale ({scale}) not recognized.", nameof(scale))
    };

    /// <summary>
    /// Plots this histogram as a multi line string.
    /// The buckets are on the downward vertical axis, and the counts are on the horizontal axis, pointing rightwards.
    /// <br/>
    /// The plot uses unicode barchart drawing characters for 8x character width precission.
    /// For a reference on unicode characters for drawing plots, see <see href="https://en.wikipedia.org/wiki/Box-drawing_character"/>.
    /// </summary>
    /// <param name="width">The maximal length of a bar in the diagram in number of characters. The atual width of the diagram will be slightly wider, to make room for the axes and ticks.</param>
    /// <param name="yTransform">Function to transform the horizontal by.</param>
    /// <param name="yInverse">The inverse of <paramref name="yTransform"/>.</param>
    /// <param name="yAbsMin">The absolute minimum value of a bucket value that is plotted. Used to avoid vertical asymptotes of <paramref name="yTransform"/>.</param>
    /// <returns></returns>
    public string PlotString(int width, Func<double, double> yTransform, Func<double, double> yInverse, int xDecimalPlaces = 0, int xMinTickWidth = 0, double yAbsMin = 0, bool trimXaxis = false)
    {
        double yMax = yInverse(Math.Max(Buckets.Skip(1).SkipLast(1).Max(), yAbsMin));
        double yMin = yInverse(yAbsMin);
        if (double.IsInfinity(yMax) || double.IsInfinity(yMin))
            throw new OverflowException("The given scale produces values too big to perform the required arithmetic.");

        // Determine number of ticks on y axis.
        int yMaxDigits = (int)Math.Log10(yTransform(yMax)) + 1;
        int yTickMinWidth = Math.Max(yMaxDigits + 1, 4);
        int yMaxTicks = width / yTickMinWidth; // If the tick step is rounded up, then the max digits may increase, making yTickMinWidth larger and hence yMaxTicks smaller
        //double yTickStep = Math.Max(Math.Ceiling((yMax - yMin) * yTickMinWidth / width), 1d); //Didn't work too great
        //int yTicks = Math.Max((int)Math.Ceiling((yMax - yMin) / yTickStep), 1);
        int yTicks = Math.Min((int)Math.Ceiling(yMax - yMin), yMaxTicks); //BUG:TODO: the fall back (right-hand side) sucks
        int yTickWidth = width / yTicks;
        double yTickStep = Math.Max(Math.Round((yMax - yMin) / width * yTickWidth), 1d);

        double yInitTickWidth = yTickWidth - .5;
        double yCharStep = yTickStep / yTickWidth;

        /// <summary>Computes the (fractional) number of bars for a specific bucket.</summary>
        double bars(double y)
        {
            double ybase = yInverse(y) - yMin;
            // First tick width is half a character shorter to align with the middle of the ticks on the horizontal axis
            return ybase < yTickStep
                ? ybase / yTickStep * yInitTickWidth
                : (ybase - yTickStep) / yCharStep + yInitTickWidth;
        }
        string drawBar(double bucket)
        {
            const string barBlocks = " ▏▎▍▌▋▊▉█";
            if (bucket <= yAbsMin)
                return "";

            double yBars = bars(bucket);
            int fullChars = (int)Math.Floor(yBars);
            // The last character is fractional (1/8) - compute fraction
            int fracBlock = (int)Math.Ceiling((yBars - fullChars) * (barBlocks.Length - 1));
            return new string(barBlocks[^1], fullChars) + barBlocks[fracBlock];
        }

        int xTickDigits = Math.Max((int)Math.Log10(XMax) + 1, xMinTickWidth) + (xDecimalPlaces > 0 ? xDecimalPlaces + 1 : 0);

        // Make lines
        List<string> lines = new List<string>(Buckets.Length - 2 + 3)
        {
            new string(' ', xTickDigits + 2) + string.Concat(Enumerable.Range(1, yTicks).Select(i => $"{yTransform(i * yTickStep + yMin):0}".PadLeft(yTickWidth))),
            $"{new string(' ', xTickDigits + 1)}┌{(new string('─', yTickWidth - 1) + "┴").Repeat(yTicks).PadRight(width, '─')}>"
        }; // skip buckets 0 and ^1

        // Draw bars

        int i = 1;
        int iLim = Buckets.Length - 2;
        if (trimXaxis)
        {
            while (iLim > 0 && string.IsNullOrEmpty(drawBar(Buckets[iLim])))
                iLim--;

            while (i <= iLim && string.IsNullOrEmpty(drawBar(Buckets[i])))
                i++;
        }

        for (; i <= iLim; i++)
            lines.Add($"{((i - 1) * XStep + XMin).ToString("F" + xDecimalPlaces).PadLeft(xTickDigits)} ┤{drawBar(Buckets[i])}");

        lines.Add($"{(iLim * XStep + XMin).ToString("F" + xDecimalPlaces).PadLeft(xTickDigits)} v");

        return string.Join(Environment.NewLine, lines);
    }
}


/// <summary>
/// The scaling mode of an axis.
/// </summary>
public enum AxisScale
{
    /// <summary>Linear</summary>
    Linear = 0,
    /// <summary>Quadratic</summary>
    Quadratic = 1,
    /// <summary>Square root</summary>
    Sqrt = ~Quadratic,

    /// <summary>Cubic</summary>
    Cubic = 2,
    /// <summary>Cube root</summary>
    Cbrt = ~Cubic,

    /// <summary>Natural exponential</summary>
    Exp = 3,
    /// <summary>Natural logarithm</summary>
    Log = ~Exp,
    /// <summary>Exponential base 10</summary>
    Exp10 = 4,
    /// <summary>Logarithm base 10</summary>
    Log10 = ~Exp10,
    /// <summary>Exponential base 2</summary>
    Exp2 = 5,
    /// <summary>Logarithm base 2</summary>
    Log2 = ~Exp2,
}
