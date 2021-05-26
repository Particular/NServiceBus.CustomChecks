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

    class CustomChecksStartup : FeatureStartupTask
    {
        public CustomChecksStartup(IEnumerable<ICustomCheck> customChecks, IMessageDispatcher dispatcher, ServiceControlBackend backend, HostInformation hostInfo, string endpointName, TimeSpan? ttl)
        {
            this.backend = backend;
            this.hostInfo = hostInfo;
            this.endpointName = endpointName;
            this.ttl = ttl;
            this.dispatcher = dispatcher;
            this.customChecks = customChecks.ToList();
        }

        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            if (!customChecks.Any())
            {
                return Task.CompletedTask;
            }

            timerPeriodicChecks = new List<TimerBasedPeriodicCheck>(customChecks.Count);
            backend.Start(dispatcher);

            foreach (var check in customChecks)
            {
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
            if (!customChecks.Any())
            {
                return;
            }

            await Task.WhenAll(timerPeriodicChecks.Select(t => t.Stop()).ToArray()).ConfigureAwait(false);
        }

        List<ICustomCheck> customChecks;
        IMessageDispatcher dispatcher;
        List<TimerBasedPeriodicCheck> timerPeriodicChecks;
        ServiceControlBackend backend;
        HostInformation hostInfo;
        string endpointName;
        TimeSpan? ttl;
    }
}