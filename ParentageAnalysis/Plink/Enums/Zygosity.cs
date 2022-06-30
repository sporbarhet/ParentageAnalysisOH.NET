namespace Sporbarhet.Parentage.Plink.Enums;

/// <summary>
/// The type of zygosity values.
/// <list type="bullet">
/// <item>0 denotes AA</item>
/// <item>1 denotes heterozygous (either AB or BA)</item>
/// <item>2 denotes BB</item>
/// <item>All other values denote NA</item>
/// </list>
/// </summary>
public enum Zygosity : byte
{
    /// <summary>Homozygous AA</summary>
    AA = 0,
    /// <summary>Heterozygous, either AB or BA</summary>
    Het = 1,
    /// <summary>Homozygous BB</summary>
    BB = 2,
    /// <summary>
    /// A missing zygosity value.
    /// Any value other than <see cref="AA"/>, <see cref="Het"/>, and <see cref="BB"/> must be considered <see cref="NA"/>.
    /// </summary>
    NA = 3
}
