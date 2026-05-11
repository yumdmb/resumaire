using System.ComponentModel.DataAnnotations;

namespace Resumaire.Api.Infrastructure.Api;

public static class ValidationExtensions
{
    public static IDictionary<string, string[]> ValidateDataAnnotations<T>(this T value)
        where T : notnull
    {
        var context = new ValidationContext(value);
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(value, context, results, validateAllProperties: true))
        {
            return new Dictionary<string, string[]>(StringComparer.Ordinal);
        }

        return results
            .SelectMany(result =>
            {
                var memberNames = result.MemberNames.Any()
                    ? result.MemberNames
                    : [string.Empty];

                return memberNames.Select(memberName => new
                {
                    MemberName = memberName,
                    Error = result.ErrorMessage ?? "The value is invalid."
                });
            })
            .GroupBy(item => item.MemberName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Error).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
    }

    public static IResult ToValidationProblem(this IDictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors);
}
