namespace Sporbarhet.Parentage.Plink.Enums;

/// <summary>
/// Defines the merging mode employed by PLINK in a --[b][p]merge operation.
/// <br/>
/// See also <seealso href="https://www.cog-genomics.org/plink/1.9/data#merge"/>.
/// </summary>
public enum MergeMode
{
    /// <summary>(default) Ignore missing calls, otherwise set mismatches to missing.</summary>
    Default = 1,
    /// <summary>Only overwrite calls which are missing in the original file.</summary>
    OverwriteMissing = 2,
    /// <summary>Only overwrite calls which are nonmissing in the new file.</summary>
    OverwriteWhenNonmissing = 3,
    /// <summary>Never overwrite.</summary>
    NeverOverwrite = 4,
    /// <summary>Always overwrite.</summary>
    AlwaysOverwrite = 5,
    /// <summary>(no merge) Report all mismatching calls.</summary>
    ReportMismatching = 6,
    /// <summary>(no merge) Report mismatching nonmissing calls.</summary>
    ReportMismatchingNonmissing = 7,
}
