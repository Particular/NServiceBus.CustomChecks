namespace NServiceBus.CustomChecks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Config;
    using NServiceBus;
    using Transports;
    using Unicast;
    using ServiceControl.Plugin.CustomChecks.Messages;

    class CustomChecksStartup : IWantToRunWhenConfigurationIsComplete, IDisposable
    {
        public CustomChecksStartup(ISendMessages dispatcher, Configure configure, UnicastBus unicastBus)
        {
            var settings = configure.Settings;
            if (!settings.TryGet("NServiceBus.CustomChecks.Queue", out string destinationQueue))
            {
                return; //HB not configured
            }
            settings.TryGet("NServiceBus.CustomChecks.Ttl", out ttl);

            var replyToAddress = !settings.GetOrDefault<bool>("Endpoint.SendOnly")
                ? settings.LocalAddress()
                : null;

            endpointName = settings.EndpointName();
            backend = new ServiceControlBackend(dispatcher, Address.Parse(destinationQueue), replyToAddress);
            this.unicastBus = unicastBus;
        }

        public void Run(Configure config)
        {
            if (backend == null)
            {
                return;
            }

            var periodicChecks = unicastBus.Builder.BuildAll<ICustomCheck>().ToList();
            foreach (var check in periodicChecks)
            {
                var checkTtl = check.Interval.HasValue
                    ? ttl ?? TimeSpan.FromTicks(check.Interval.Value.Ticks * 4)
                    : TimeSpan.MaxValue;

                var checkRunner = new TimerBasedPeriodicCheck(check, backend, (id, category, result) => new ReportCustomCheckResult
                {
                    CustomCheckId = id,
                    Category = category,
                    HasFailed = result.HasFailed,
                    FailureReason = result.FailureReason,
                    ReportedAt = DateTime.UtcNow,
                    EndpointName = endpointName,
                    Host = unicastBus.HostInformation.DisplayName,
                    HostId = unicastBus.HostInformation.HostId
                }, checkTtl);
                timerPeriodicChecks.Add(checkRunner);
            }
        }

        public void Stop()
        {
            Parallel.ForEach(timerPeriodicChecks, t => t.Dispose());
        }

        List<TimerBasedPeriodicCheck> timerPeriodicChecks = new List<TimerBasedPeriodicCheck>();
        UnicastBus unicastBus;
        ServiceControlBackend backend;
        string endpointName;
        TimeSpan? ttl;

        public void Dispose()
        {
            foreach (var periodicCheck in timerPeriodicChecks)
            {
                periodicCheck.Dispose();
            }
        }
    }
}