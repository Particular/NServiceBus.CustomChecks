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
    class CustomChecksStartup(
        IReadOnlyList<ICustomCheckWrapper> wrappers,
        IMessageDispatcher dispatcher,
        ServiceControlBackend backend,
        HostInformation hostInfo,
        string endpointName,
        TimeSpan? ttl)
        : FeatureStartupTask
    {
        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            if (wrappers.Count == 0)
            {
                return Task.CompletedTask;
            }

            timerPeriodicChecks = new List<TimerBasedPeriodicCheck>(wrappers.Count);
            backend.Start(dispatcher);

            foreach (var wrapper in wrappers)
            {
                var check = wrapper.Instance;
                var checkTtl = check.Interval.HasValue
                    ? ttl ?? TimeSpan.FromTicks(check.Interval.Value.Ticks * 4)
                    : TimeSpan.MaxValue;

                var timerBasedPeriodicCheck = new TimerBasedPeriodicCheck(check, backend, (result) => new ReportCustomCheckResult
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
            if (wrappers.Count == 0)
            {
                return;
            }

            await Task.WhenAll(timerPeriodicChecks.Select(t => t.Stop(cancellationToken)).ToArray()).ConfigureAwait(false);

            foreach (var wrapper in wrappers)
            {
                await wrapper.DisposeAsync().ConfigureAwait(false);
            }
        }

        List<TimerBasedPeriodicCheck> timerPeriodicChecks;
    }
}