namespace NServiceBus.CustomChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Features;
using Hosting;
using ServiceControl.Plugin.CustomChecks.Messages;
using Transport;

sealed class CustomChecksStartup(
    IReadOnlyCollection<ICustomCheckWrapper> checks,
    IMessageDispatcher dispatcher,
    ServiceControlBackend backend,
    HostInformation hostInfo,
    string endpointName,
    TimeSpan? ttl)
    : FeatureStartupTask
{
    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
    {
        if (checks.Count == 0)
        {
            return Task.CompletedTask;
        }

        timerPeriodicChecks = new List<TimerBasedPeriodicCheck>(checks.Count);
        backend.Start(dispatcher);

        var customCheckHostInfo = new CustomCheckHostInfo(endpointName, hostInfo.DisplayName, hostInfo.HostId);

        foreach (var check in checks)
        {
            var checkTtl = check.Interval.HasValue
                ? ttl ?? TimeSpan.FromTicks(check.Interval.Value.Ticks * 4)
                : TimeSpan.MaxValue;

            var timerBasedPeriodicCheck = new TimerBasedPeriodicCheck(check, backend, static (hostInfo, check, result) => new ReportCustomCheckResult
            {
                CustomCheckId = check.Id,
                Category = check.Category,
                HasFailed = result.HasFailed,
                FailureReason = result.FailureReason,
                ReportedAt = DateTime.UtcNow,
                EndpointName = hostInfo.EndpointName,
                Host = hostInfo.HostDisplayName,
                HostId = hostInfo.HostId
            }, checkTtl, customCheckHostInfo);

            timerBasedPeriodicCheck.Start();

            timerPeriodicChecks.Add(timerBasedPeriodicCheck);
        }

        return Task.CompletedTask;
    }

    protected override async Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
    {
        if (timerPeriodicChecks is null)
        {
            return;
        }

        if (timerPeriodicChecks.Count == 0)
        {
            return;
        }

        try
        {
            await Task.WhenAll([.. timerPeriodicChecks.Select(t => t.Stop(cancellationToken))]).ConfigureAwait(false);
        }
        finally
        {
            foreach (var disposable in timerPeriodicChecks)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    List<TimerBasedPeriodicCheck>? timerPeriodicChecks;
}