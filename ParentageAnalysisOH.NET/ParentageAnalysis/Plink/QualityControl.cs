using System.Globalization;

namespace Sporbarhet.Parentage.Plink;

/// <summary>
/// PLINK quality control parameters. See also <seealso href="https://www.cog-genomics.org/plink/1.9/filter"/>.
/// <br/>
/// See <see href="https://www.cog-genomics.org/plink/1.9/filter"/> for more details and other options.
/// The documentation pages for PLINK 1.07 may also be helpful.
/// </summary>
public class QualityControl : IEquatable<QualityControl?>
{
    public bool Nonfounders { get; } = true;
    /// <summary>
    /// Filter out all variants with missing call rates exceeding this value.
    /// Set to <c>1</c> to disable.
    /// </summary>
    public double Geno { get; } = 1;
    /// <summary>
    /// Filter out all samples with missing call rates exceeding this value.
    /// Set to <c>1</c> to disable.
    /// </summary>
    public double Mind { get; } = 1;
    /// <summary>
    /// Filter out all variants with minor allele frequency below this value.
    /// Set to <c>0</c> to disable.
    /// </summary>
    public double Maf { get; } = 0;
    /// <summary>
    /// Filter out all variants with minor allele frequency above this value.
    /// Set to <c>1</c> to disable.
    /// </summary>
    public double MaxMaf { get; } = 1;
    /// <summary>
    /// Filter out all variants with minor allele count below this value.
    /// Set to <c>0</c> to disable.
    /// </summary>
    public int Mac { get; } = 0;
    /// <summary>
    /// Filter out all variants with minor allele count above this value.
    /// Set to <c>-1</c> to disable.
    /// </summary>
    public int MaxMac { get; } = -1;
    /// <summary>
    /// Hardy-Weinberg equilibrium test.
    /// Filter out all variants which have Hardy-Weinberg exact test p-value below this value.
    /// Set to <c>0</c> to disable.
    /// <br/>
    /// See also <seealso href="https://www.cog-genomics.org/plink/1.9/filter#hwe"/>.
    /// </summary>
    public double HWe { get; } = 0;
    /// <summary>
    /// Whether to use the 'midp' option for the Hardy-Weinberg equilibrium test. Its use is recommended,
    /// see <see href="https://www.cog-genomics.org/plink/1.9/filter#hwe"/> for details.
    /// </summary>
    public bool HWeMidP { get; } = true;
    /// <summary>
    /// Thin removes variants a at random by retaining each variant with a probability equal to this field.
    /// Set to <c>1</c> to disable.
    /// <br/>
    /// See also <seealso href="https://www.cog-genomics.org/plink/1.9/filter#thin"/>.
    /// </summary>
    public double Thin { get; } = 1;


    /// <summary>
    /// Generates the PLINK flags for performing the quality control specified by <see cref="this"/> <see cref="QualityControl"/> instance.
    /// </summary>
    /// <returns>The PLINK flags for performing the quality control specified by <see cref="this"/> <see cref="QualityControl"/> instance.</returns>
    public IEnumerable<string> GetPlinkFlags()
    {
        if (Nonfounders)
            yield return "--nonfounders";

        if (Geno < 1 && !double.IsNaN(Geno))
            yield return "--geno " + Geno.ToString(CultureInfo.InvariantCulture);
        if (Mind < 1 && !double.IsNaN(Mind))
            yield return "--mind " + Mind.ToString(CultureInfo.InvariantCulture);

        if (Maf > 0 && !double.IsNaN(Maf))
            yield return "--maf " + Maf.ToString(CultureInfo.InvariantCulture);
        if (MaxMaf < 1 && !double.IsNaN(MaxMaf))
            yield return "--max-maf " + MaxMaf.ToString(CultureInfo.InvariantCulture);
        if (Mac > 0)
            yield return "--mac " + Mac.ToString(CultureInfo.InvariantCulture);
        if (MaxMac >= 0)
            yield return "--max-mac " + MaxMac.ToString(CultureInfo.InvariantCulture);

        if (HWe > 0 && !double.IsNaN(HWe))
            yield return "--hwe " + HWe.ToString(CultureInfo.InvariantCulture) + (HWeMidP ? " midp" : "");

        if (Thin < 1 && !double.IsNaN(Thin))
            yield return "--thin " + Thin.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Constructs the PLINK flag string for performing the quality control specified by <see cref="this"/> <see cref="QualityControl"/> instance.
    /// See also <seealso cref="GetPlinkFlags"/>.
    /// </summary>
    /// <returns>The PLINK flag string for performing the quality control specified by <see cref="this"/> <see cref="QualityControl"/> instance.</returns>
    public string GetPlinkCommand() => string.Join(' ', GetPlinkFlags());


    public QualityControl(bool Nonfounders = true, double Geno = 1, double Mind = 1, double Maf = 0, double MaxMaf = 1, int Mac = 0, int MaxMac = -1, double HWe = 0, bool HWeMidP = true, double Thin = 1)
    {
        if (Geno is < 0 or > 1 || double.IsNaN(Geno))
            throw new ArgumentOutOfRangeException(nameof(Geno), $"A value of {Geno} was given, but {nameof(Geno)} must be in the inclusive interval [0, 1]. A value of 1 disables it.");
        if (Mind is < 0 or > 1 || double.IsNaN(Mind))
            throw new ArgumentOutOfRangeException(nameof(Mind), $"A value of {Mind} was given, but {nameof(Mind)} must be in the inclusive interval [0, 1]. A value of 1 disables it.");
        if (Maf is < 0 or > 1 || double.IsNaN(Maf))
            throw new ArgumentOutOfRangeException(nameof(Maf), $"A value of {Maf} was given, but {nameof(Maf)} must be in the inclusive interval [0, 1]. A value of 0 disables it.");
        if (MaxMaf is < 0 or > 1 || double.IsNaN(MaxMaf))
            throw new ArgumentOutOfRangeException(nameof(MaxMaf), $"A value of {MaxMaf} was given, but {nameof(MaxMaf)} must be in the inclusive interval [0, 1]. A value of 1 disables it.");
        if (MaxMaf < Maf)
            throw new ArgumentException($"The parameter {nameof(Maf)} must be less than or equal to {nameof(MaxMaf)}, but a value of {Maf} was given for {nameof(Maf)} while a value of {MaxMaf} was given for {nameof(MaxMaf)}.", nameof(Maf));
        if (Mac < 0)
            throw new ArgumentOutOfRangeException(nameof(Mac), $"A value of {Mac} was given, but {nameof(Mac)} must be non-negative. A value of 0 disables it.");
        if (MaxMac < -1)
            throw new ArgumentOutOfRangeException(nameof(MaxMac), $"A value of {MaxMac} was given, but {nameof(MaxMac)} must be greater than or equal to -1. A value of -1 disables it.");
        if (MaxMac < Mac && MaxMac >= 0)
            throw new ArgumentException($"The parameter {nameof(Mac)} must be less than or equal to {nameof(MaxMac)}, but a value of {Mac} was given for {nameof(Mac)} while a value of {MaxMac} was given for {nameof(MaxMac)}.", nameof(Mac));
        if (HWe is < 0 or >= 1 || double.IsNaN(HWe))
            throw new ArgumentOutOfRangeException(nameof(HWe), $"A value of {HWe} was given, but {nameof(HWe)} must be in the inclusive interval [0, 1]. A value of 0 disables it.");
        if (Thin is <= 0 or > 1 || double.IsNaN(Thin))
            throw new ArgumentOutOfRangeException(nameof(Thin), $"A value of {Thin} was given, but {nameof(Thin)} must be in the inclusive interval [0, 1]. A value of 1 disables it.");

        this.Nonfounders = Nonfounders;
        this.Geno = Geno;
        this.Mind = Mind;
        this.Maf = Maf;
        this.MaxMaf = MaxMaf;
        this.Mac = Mac;
        this.MaxMac = MaxMac;
        this.HWe = HWe;
        this.HWeMidP = HWeMidP;
        this.Thin = Thin;
    }

    /// <summary>
    /// A <see cref="QualityControl"/> instance specifying that no quality control is to be done.
    /// </summary>
    public static readonly QualityControl NoQualityControl = new QualityControl(false, 1, 1, 0, 1, 0, -1, 0, true, 1);


    public override bool Equals(object? obj) => Equals(obj as QualityControl);
    public bool Equals(QualityControl? other) => other is not null && Nonfounders == other.Nonfounders && Geno == other.Geno && Mind == other.Mind && Maf == other.Maf && MaxMaf == other.MaxMaf && Mac == other.Mac && MaxMac == other.MaxMac && HWe == other.HWe && HWeMidP == other.HWeMidP && Thin == other.Thin;
    public static bool operator ==(QualityControl? left, QualityControl? right) => EqualityComparer<QualityControl>.Default.Equals(left, right);
    public static bool operator !=(QualityControl? left, QualityControl? right) => !(left == right);
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(Nonfounders);
        hash.Add(Geno);
        hash.Add(Mind);
        hash.Add(Maf);
        hash.Add(MaxMaf);
        hash.Add(Mac);
        hash.Add(MaxMac);
        hash.Add(HWe);
        hash.Add(HWeMidP);
        hash.Add(Thin);
        return hash.ToHashCode();
    }
}
