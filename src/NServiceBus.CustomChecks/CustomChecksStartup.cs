namespace NServiceBus.CustomChecks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Features;
    using Hosting;
    using ServiceControl.Plugin.CustomChecks.Messages;
    using Transport;

    class CustomChecksStartup : FeatureStartupTask
    {
        public CustomChecksStartup(IEnumerable<ICustomCheck> customChecks, IDispatchMessages dispatcher, ServiceControlBackend backend, HostInformation hostInfo, string endpointName)
        {
            this.backend = backend;
            this.hostInfo = hostInfo;
            this.endpointName = endpointName;
            this.dispatcher = dispatcher;
            this.customChecks = customChecks.ToList();
        }

        protected override Task OnStart(IMessageSession session)
        {
            if (!customChecks.Any())
            {
                return Task.FromResult(0);
            }

            timerPeriodicChecks = new List<TimerBasedPeriodicCheck>(customChecks.Count);
            backend.Start(dispatcher);

            foreach (var check in customChecks)
            {
                var timerBasedPeriodicCheck = new TimerBasedPeriodicCheck(check, backend, (id, category, result) => new ReportCustomCheckResult
                {
                    CustomCheckId = id,
                    Category = category,
                    HasFailed = result.HasFailed,
                    FailureReason = result.FailureReason,
                    ReportedAt = DateTime.UtcNow,
                    EndpointName = endpointName,
                    Host = hostInfo.DisplayName,
                    HostId = hostInfo.HostId
                });
                timerBasedPeriodicCheck.Start();

                timerPeriodicChecks.Add(timerBasedPeriodicCheck);
            }
            return Task.FromResult(0);
        }

        protected override async Task OnStop(IMessageSession session)
        {
            if (!customChecks.Any())
            {
                return;
            }

            await Task.WhenAll(timerPeriodicChecks.Select(t => t.Stop()).ToArray()).ConfigureAwait(false);
        }

        List<ICustomCheck> customChecks;
        IDispatchMessages dispatcher;
        List<TimerBasedPeriodicCheck> timerPeriodicChecks;
        ServiceControlBackend backend;
        HostInformation hostInfo;
        string endpointName;
    }
}