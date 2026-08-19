namespace DocumentationLoggingDashboard.QAReports.Validation;

public sealed class QuickQaValidationResult
{
    public QuickQaValidationResult(IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Select(error => error.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;
}
