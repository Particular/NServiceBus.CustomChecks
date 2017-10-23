namespace NServiceBus.CustomChecks
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using ServiceControl.Plugin.CustomChecks.Messages;

    class TimerBasedPeriodicCheck
    {
        static ILog Logger = LogManager.GetLogger(typeof(TimerBasedPeriodicCheck));

        public TimerBasedPeriodicCheck(ICustomCheck customCheck, ServiceControlBackend serviceControlBackend, Func<string, string, CheckResult, ReportCustomCheckResult> messageFactory)
        {
            this.customCheck = customCheck;
            this.serviceControlBackend = serviceControlBackend;
            this.messageFactory = messageFactory;
        }

        public void Start()
        {
            timer = new AsyncTimer();
            timer.Start(Run, customCheck.Interval, e => { /* should not happen */});
        }

        public Task Stop()
        {
            return timer.Stop();
        }

        async Task Run()
        {
            CheckResult result;
            try
            {
                result = await customCheck.PerformCheck().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var reason = $"'{customCheck.GetType()}' implementation failed to run.";
                result = CheckResult.Failed(reason);
                Logger.Error(reason, ex);
            }

            try
            {
                var checkResult = messageFactory(customCheck.Id, customCheck.Category, result);
                await serviceControlBackend.Send(checkResult, customCheck.Interval.HasValue ? TimeSpan.FromTicks( customCheck.Interval.Value.Ticks*4) : TimeSpan.MaxValue).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to report periodic check to ServiceControl.", ex);
            }
        }

        ICustomCheck customCheck;
        ServiceControlBackend serviceControlBackend;
        Func<string, string, CheckResult, ReportCustomCheckResult> messageFactory;
        AsyncTimer timer;
    }
}