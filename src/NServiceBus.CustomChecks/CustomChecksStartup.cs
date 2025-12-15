namespace NServiceBus.CustomChecks
{
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
        IReadOnlyCollection<ICustomCheck> checks,
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

            foreach (var check in checks)
            {
                var checkTtl = check.Interval.HasValue
                    ? ttl ?? TimeSpan.FromTicks(check.Interval.Value.Ticks * 4)
                    : TimeSpan.MaxValue;

                var timerBasedPeriodicCheck = new TimerBasedPeriodicCheck(check, backend, result => new ReportCustomCheckResult
                {
                    CustomCheckId = check.Id,
                    Category = check.Category,
                    HasFailed = result.HasFailed,
                    FailureReason = result.FailureReason,
                    ReportedAt = DateTime.UtcNow,
                    EndpointName = endpointName,
                    Host = hostInfo.DisplayName,
                    HostId = hostInfo.HostId
                }, checkTtl);

                timerBasedPeriodicCheck.Start();

                timerPeriodicChecks.Add(timerBasedPeriodicCheck);
            }

            return Task.CompletedTask;
        }

        protected override async Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            if (checks.Count == 0)
            {
                return;
            }

            try
            {
                await Task.WhenAll(timerPeriodicChecks.Select(t => t.Stop(cancellationToken)).ToArray()).ConfigureAwait(false);
            }
            finally
            {
                foreach (var disposable in checks.OfType<IAsyncDisposable>())
                {
                    await disposable.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        List<TimerBasedPeriodicCheck> timerPeriodicChecks;
    }
}