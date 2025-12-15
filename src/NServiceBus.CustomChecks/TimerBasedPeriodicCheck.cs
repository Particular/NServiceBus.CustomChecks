namespace NServiceBus.CustomChecks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using ServiceControl.Plugin.CustomChecks.Messages;

    sealed class TimerBasedPeriodicCheck(
        ICustomCheckWrapper check,
        ServiceControlBackend messageSender,
        Func<CheckResult, ReportCustomCheckResult> messageFactory,
        TimeSpan ttl) : IAsyncDisposable
    {
        static readonly ILog Logger = LogManager.GetLogger<TimerBasedPeriodicCheck>();

        CancellationTokenSource stopTokenSource;

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

        public ValueTask DisposeAsync()
        {
            stopTokenSource?.Dispose();
            return check.DisposeAsync();
        }

        async Task RunAndSwallowExceptions(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
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
                catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
                {
                    // private token, check is being stopped, log the exception in case the stack trace is ever needed for debugging
                    Logger.Debug("Operation canceled while stopping custom check.", ex);
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
