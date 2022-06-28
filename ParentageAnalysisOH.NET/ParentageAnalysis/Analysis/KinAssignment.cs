using Microsoft.Data.Analysis;
using System.Globalization;

using TOH = System.Int32;

namespace Sporbarhet.Parentage.Analysis;
/// <summary>
/// Holds the information regarding potential male and female relatives of a given subject based on the opposing homozygous loci counts.
/// </summary>
public class KinAssignment
{
    /// <summary>
    /// The id of the subject sample. The subject is what we are interested in finding relations to.
    /// </summary>
    public ID Subject;
    /// <summary>
    /// Prioritized list of potential kin.
    /// </summary>
    public IReadOnlyList<(ID Id, TOH Oh)> Kin;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Input dictionaries must be sorted on values.
    /// </remarks>
    /// <param name="subjectId"></param>
    /// <param name="kin"></param>
    KinAssignment(ID subjectId, IReadOnlyList<(ID, TOH)>? kin)
    {
        Subject = subjectId;
        Kin = kin ?? Array.Empty<(ID, TOH)>();
    }

    /// <summary>
    /// Assigns the most likely parents to the given offspring out of the given parents. Repeat for male and female parent.
    /// </summary>
    /// <param name="kinIds">Collection of potential parents ids. Either male or female parent.</param>
    /// <param name="ohThreshold"></param>
    /// <returns></returns>
    protected static IEnumerable<(ID ID, int OH)> FindKin<OH>(IEnumerable<ID> kinIds, OH ohThreshold, IEnumerable<OH> subjectOhCounts, ID subjectId)
        where OH : unmanaged, IComparable<OH>
        => kinIds.AsParallel().Zip(subjectOhCounts) // Map locations to matrix entries
                    .Where(idOh => ohThreshold.CompareTo(idOh.Second) > 0 && idOh.First != subjectId) // Keep only entries below ohThreshold
                    .OrderBy(idOh => idOh.Second) // Sort by OH count
                    .Select(idOh => (idOh.First, Convert.ToInt32(idOh.Second))); // Convert to integers for less headache later. We do not need to save memory anymore


    public static KinAssignment AssignKin<OH>(ID subjectId, IEnumerable<ID> kinIds, OH ohThreshold, IEnumerable<OH> subjectOhCounts)
        where OH : unmanaged, IComparable<OH>
        => new KinAssignment(subjectId, FindKin(kinIds, ohThreshold, subjectOhCounts, subjectId).ToList());


    /// <summary>
    /// </summary>
    /// <param name="subjectIds"></param>
    /// <param name="kinIds"></param>
    /// <param name="ohThreshold"></param>
    /// <param name="ohCounts"></param>
    /// <returns></returns>
    public static IEnumerable<KinAssignment> AssignKin<OH>(IEnumerable<ID> subjectIds, IEnumerable<ID> kinIds, OH ohThreshold, IReadOnlyList<IEnumerable<OH>> ohCounts)
        where OH : unmanaged, IComparable<OH>
        => subjectIds.Select((offId, o) => AssignKin(offId, kinIds, ohThreshold, ohCounts[o]));


    /// <summary>
    /// </summary>
    /// <param name="subjectIds"></param>
    /// <param name="kinIds"></param>
    /// <param name="ohThreshold"></param>
    /// <param name="ohCounts"></param>
    /// <returns></returns>
    public static IEnumerable<KinAssignment> AssignKin<OH>(IEnumerable<ID> subjectIds, IEnumerable<ID> kinIds, OH ohThreshold, OH[,] ohCounts)
        where OH : unmanaged, IComparable<OH>
        => subjectIds.AsParallel().Select((subjId, s) => AssignKin(subjId, kinIds, ohThreshold, kinIds.Select((_, i) => ohCounts[s, i])));

    /// <summary>
    /// </summary>
    /// <param name="subjectIds"></param>
    /// <param name="kinIds"></param>
    /// <param name="ohThreshold"></param>
    /// <param name="ohCounts"></param>
    /// <returns></returns>
    public static IEnumerable<KinAssignment> AssignKinTransposed<OH>(IEnumerable<ID> subjectIds, IEnumerable<ID> kinIds, OH ohThreshold, IEnumerable<IReadOnlyList<OH>> ohCounts)
        where OH : unmanaged, IComparable<OH>
        => subjectIds.Select((offId, o) => AssignKin(offId, kinIds, ohThreshold, ohCounts.Select(oh => oh[o])));


    /// <summary>
    /// </summary>
    /// <param name="subjectIds"></param>
    /// <param name="kinIds"></param>
    /// <param name="ohThreshold"></param>
    /// <param name="ohCounts"></param>
    /// <returns></returns>
    public static IEnumerable<KinAssignment> AssignKinTransposed<OH>(IEnumerable<ID> subjectIds, IEnumerable<ID> kinIds, OH ohThreshold, OH[,] ohCounts)
        where OH : unmanaged, IComparable<OH>
        => subjectIds.AsParallel().Select((subjId, s) => AssignKin(subjId, kinIds, ohThreshold, kinIds.Select((_, i) => ohCounts[i, s])));


    public static DataFrame CreateDataFrame(IEnumerable<KinAssignment> assignments, IReadOnlyDictionary<ID, Sex> sexes)
        => new DataFrame(
            DataFrameColumn.Create("SubjectID", assignments.SelectMany(pa => Enumerable.Repeat(pa.Subject, pa.Kin.Count))),
            DataFrameColumn.Create("KinID", assignments.SelectMany(pa => pa.Kin.Select(p => p.Id))),
            DataFrameColumn.Create("KinSex", assignments.SelectMany(pa => pa.Kin.Select(p => sexes[p.Id]))),
            DataFrameColumn.Create("OH", assignments.SelectMany(pa => pa.Kin.Select(p => p.Oh)))
        );


    public static DataFrame CreateDataFrame(IEnumerable<KinAssignment> assignments, IDictionary<ID, Sex> sexes)
        => new DataFrame(
            DataFrameColumn.Create("SubjectID", assignments.SelectMany(pa => Enumerable.Repeat(pa.Subject, pa.Kin.Count))),
            DataFrameColumn.Create("KinID", assignments.SelectMany(pa => pa.Kin.Select(p => p.Id))),
            DataFrameColumn.Create("KinSex", assignments.SelectMany(pa => pa.Kin.Select(p => sexes[p.Id]))), //TODO: maybe subject sex instead
            DataFrameColumn.Create("OH", assignments.SelectMany(pa => pa.Kin.Select(p => p.Oh)))
        );


    public static void WriteAssignments(string assignPath, IEnumerable<KinAssignment> asignments, IReadOnlyDictionary<ID, Sex> sexes)
    {
        DataFrame? dfAssignments = CreateDataFrame(asignments, sexes);
        DataFrame.WriteCsv(dfAssignments, assignPath, ',', true, cultureInfo: CultureInfo.InvariantCulture);
    }


    public static void WriteAssignments(string assignPath, IEnumerable<KinAssignment> assignments, IDictionary<ID, Sex> sexes)
    {
        DataFrame? dfAssignments = CreateDataFrame(assignments, sexes);
        DataFrame.WriteCsv(dfAssignments, assignPath, ',', true, cultureInfo: CultureInfo.InvariantCulture);
    }
}
