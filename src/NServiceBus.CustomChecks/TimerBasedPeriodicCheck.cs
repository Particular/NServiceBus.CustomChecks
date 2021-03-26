namespace NServiceBus.CustomChecks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using ServiceControl.Plugin.CustomChecks.Messages;

    class TimerBasedPeriodicCheck
    {
        static ILog Logger = LogManager.GetLogger(typeof(TimerBasedPeriodicCheck));

        public TimerBasedPeriodicCheck(ICustomCheck customCheck, ServiceControlBackend serviceControlBackend, Func<string, string, CheckResult, ReportCustomCheckResult> messageFactory, TimeSpan ttl)
        {
            this.customCheck = customCheck;
            this.serviceControlBackend = serviceControlBackend;
            this.messageFactory = messageFactory;
            this.ttl = ttl;
        }

        public void Start()
        {
            stopPeriodicChecksTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!customCheck.Interval.HasValue)
                    {
                        await Run(stopPeriodicChecksTokenSource.Token).ConfigureAwait(false);
                        return;
                    }

                    while (!stopPeriodicChecksTokenSource.IsCancellationRequested)
                    {
                        await Run(stopPeriodicChecksTokenSource.Token).ConfigureAwait(false);

                        await Task.Delay(customCheck.Interval.Value, stopPeriodicChecksTokenSource.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (stopPeriodicChecksTokenSource.IsCancellationRequested)
                {
                    //no-op
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to run periodic custom checks", ex);
                }
            }, CancellationToken.None);
        }

        public Task Stop()
        {
            stopPeriodicChecksTokenSource?.Cancel();

            return Task.CompletedTask;
        }

        async Task Run(CancellationToken cancellationToken)
        {
            CheckResult result;
            try
            {
                result = await customCheck.PerformCheck(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                //no-op
                return;
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
                await serviceControlBackend.Send(checkResult, ttl, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to report periodic check to ServiceControl.", ex);
            }
        }

        CancellationTokenSource stopPeriodicChecksTokenSource;
        ICustomCheck customCheck;
        ServiceControlBackend serviceControlBackend;
        Func<string, string, CheckResult, ReportCustomCheckResult> messageFactory;
        TimeSpan ttl;
    }
}