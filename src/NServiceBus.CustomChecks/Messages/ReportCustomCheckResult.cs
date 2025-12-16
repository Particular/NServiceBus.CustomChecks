namespace ServiceControl.Plugin.CustomChecks.Messages;

using System;
using System.Diagnostics.CodeAnalysis;

class ReportCustomCheckResult
{
    public required Guid HostId { get; set; }

    public required string CustomCheckId { get; set; }

    public required string Category { get; set; }

    public bool HasFailed { get; set; }

    [MemberNotNullWhen(true, nameof(HasFailed))]
    public string? FailureReason { get; set; }

    public DateTime ReportedAt { get; set; }

    public required string EndpointName { get; set; }

    public required string Host { get; set; }
}