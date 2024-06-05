using ElectionResults.Hangfire.Apis.RoAep.Models;

namespace ElectionResults.Hangfire.Extensions;

public static class FieldsExtensions
{
    public static int TryGetTotalNumberOfEligibleVoters(this List<FieldModel> fields)
    {
        var totalNumberOfEligibleVoters = fields.FirstOrDefault(x => x.Name == FieldNames.a)?.Value ?? 0;
        return totalNumberOfEligibleVoters;
    }

    public static int TryGetNumberOfVotes(this List<FieldModel> fields)
    {
        var totalNumberOfVotes = fields.FirstOrDefault(x => x.Name == FieldNames.b)?.Value ?? 0;
        return totalNumberOfVotes;
    }

    public static int TryGetNumberOfValidVotes(this List<FieldModel> fields)
    {
        var totalNumberOfVotes = fields.FirstOrDefault(x => x.Name == FieldNames.c)?.Value ?? 0;
        return totalNumberOfVotes;
    }

    public static int TryGetNumberOfNullVotes(this List<FieldModel> fields)
    {
        var totalNumberOfVotes = fields.FirstOrDefault(x => x.Name == FieldNames.d)?.Value ?? 0;
        return totalNumberOfVotes;
    }
}