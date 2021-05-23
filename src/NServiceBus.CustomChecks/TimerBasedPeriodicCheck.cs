namespace NServiceBus.CustomChecks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using ServiceControl.Plugin.CustomChecks.Messages;

    class TimerBasedPeriodicCheck
    {
        static readonly ILog Logger = LogManager.GetLogger<TimerBasedPeriodicCheck>();

        readonly ICustomCheck check;
        readonly ServiceControlBackend messageSender;
        readonly Func<CheckResult, ReportCustomCheckResult> messageFactory;
        readonly TimeSpan ttl;

        CancellationTokenSource stopTokenSource;

        public TimerBasedPeriodicCheck(ICustomCheck check, ServiceControlBackend messageSender, Func<CheckResult, ReportCustomCheckResult> messageFactory, TimeSpan ttl)
        {
            this.check = check;
            this.messageSender = messageSender;
            this.messageFactory = messageFactory;
            this.ttl = ttl;
        }

        public void Start()
        {
            stopTokenSource = new CancellationTokenSource();

            _ = RunAndSwallowExceptions(stopTokenSource.Token);
        }

        public Task Stop(CancellationToken cancellationToken = default)
        {
            stopTokenSource?.Cancel();

            return Task.CompletedTask;
        }

        async Task RunAndSwallowExceptions(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var result = await InvokeAndWrapFailure(check, cancellationToken).ConfigureAwait(false);

                        var message = messageFactory(result);

                        await SendAndWarnOnFailure(messageSender, message, ttl, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
                    {
                        Logger.Error("Custom check failed but can be retried.", ex);
                    }

                    if (!check.Interval.HasValue)
                    {
                        break;
                    }

                    await Task.Delay(check.Interval.Value, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (ex.IsCausedBy(cancellationToken))
                {
                    Logger.Debug("Custom check canceled.", ex);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error("Custom check failed and cannot be retried.", ex);
                    break;
                }
            }
        }

        static async Task SendAndWarnOnFailure(ServiceControlBackend sender, ReportCustomCheckResult message, TimeSpan ttl, CancellationToken cancellationToken)
        {
            try
            {
                await sender.Send(message, ttl, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
            {
                Logger.Warn("Failed to send periodic check to ServiceControl.", ex);
            }
        }

        static async Task<CheckResult> InvokeAndWrapFailure(ICustomCheck check, CancellationToken cancellationToken)
        {
            try
            {
                return await check.PerformCheck(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
            {
                var reason = $"'{check.GetType()}' implementation failed to run.";
                Logger.Error(reason, ex);
                return CheckResult.Failed(reason);
            }
        }
    }
}
