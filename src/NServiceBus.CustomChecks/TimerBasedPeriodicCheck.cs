namespace NServiceBus.CustomChecks
{
    using System;
    using System.Threading;
    using Logging;
    using ServiceControl.Plugin.CustomChecks.Messages;

    class TimerBasedPeriodicCheck : IDisposable
    {
        public TimerBasedPeriodicCheck(ICustomCheck customCheck, ServiceControlBackend serviceControlBackend, 
            Func<string, string, CheckResult, ReportCustomCheckResult> messageFactory,
            TimeSpan ttl)
        {
            this.customCheck = customCheck;
            this.serviceControlBackend = serviceControlBackend;
            this.messageFactory = messageFactory;
            this.ttl = ttl;

            timer = new Timer(Run, null, TimeSpan.Zero, customCheck.Interval ?? TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                timer.Dispose(waitHandle);

                waitHandle.WaitOne();
            }
        }

        void Run(object state)
        {
            CheckResult result;
            try
            {
                result = customCheck.PerformCheck();
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
                serviceControlBackend.Send(checkResult, ttl);
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to report periodic check to ServiceControl.", ex);
            }
        }

        static ILog Logger = LogManager.GetLogger(typeof(TimerBasedPeriodicCheck));
        ICustomCheck customCheck;
        ServiceControlBackend serviceControlBackend;
        Func<string, string, CheckResult, ReportCustomCheckResult> messageFactory;
        TimeSpan ttl;
        Timer timer;
    }
}