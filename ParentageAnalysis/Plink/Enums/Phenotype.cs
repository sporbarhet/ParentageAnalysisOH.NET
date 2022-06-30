namespace Sporbarhet.Parentage.Plink.Enums;

/// <summary>
/// The phenotypes of samples in a dataset are used to separate them into groups with different characteristics.
/// <br/>
/// <example>
/// For our purposes, it may for instance be useful to define parents as controls and offspring as cases.
/// </example>
/// </summary>
public enum Phenotype
{
    /// <summary>
    /// Any value of 0 or less should be considered as missing. Another typical missing-value is -9.
    /// </summary>
    Missing = 0,
    /// <summary>
    /// Signifies that a sample is unaffected(control).
    /// </summary>
    Control = 1,
    /// <summary>
    /// Signifies that a sample is affected(case).
    /// </summary>
    Case = 2,

    /// <summary>
    /// A value to denote that the current phenotypes should not be overwritten. Used internally.
    /// </summary>
    DontSet = ~0,
}
