namespace NServiceBus.CustomChecks;

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

/// <summary>
/// The result of a check.
/// </summary>
public class CheckResult
{
    /// <summary>
    /// <code>true</code> if it failed.
    /// </summary>
    public bool HasFailed { get; private init; }

    /// <summary>
    /// The reason for the failure.
    /// </summary>
    [MemberNotNullWhen(true, nameof(HasFailed))]
    public string? FailureReason { get; private init; }

    /// <summary>
    /// Passes a check.
    /// </summary>
    public static readonly CheckResult Pass = new();

    /// <summary>
    /// Fails a check.
    /// </summary>
    /// <param name="reason">Reason for failure.</param>
    /// <returns>The result.</returns>
    public static CheckResult Failed(string reason) =>
        new()
        {
            HasFailed = true,
            FailureReason = reason
        };

    /// <summary>
    /// Converts a check result.
    /// </summary>
    /// <param name="result">The converted result.</param>
    public static implicit operator Task<CheckResult>(CheckResult result) => Task.FromResult(result);
}