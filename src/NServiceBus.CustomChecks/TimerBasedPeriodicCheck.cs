namespace NServiceBus.CustomChecks;

using System;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using ServiceControl.Plugin.CustomChecks.Messages;

sealed class TimerBasedPeriodicCheck(
    ICustomCheckWrapper check,
    ServiceControlBackend messageSender,
    Func<CustomCheckHostInfo, ICustomCheck, CheckResult, ReportCustomCheckResult> messageFactory,
    TimeSpan ttl,
    CustomCheckHostInfo customCheckHostInfo) : IAsyncDisposable
{
    static readonly ILog Logger = LogManager.GetLogger<TimerBasedPeriodicCheck>();

    CancellationTokenSource? stopTokenSource;
    Task? timerTask;

    public void Start()
    {
        stopTokenSource = new CancellationTokenSource();

        timerTask = RunAndSwallowExceptions(stopTokenSource.Token);
    }

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        stopTokenSource?.Cancel();

        if (timerTask is null)
        {
            return;
        }

        try
        {
            await timerTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException e) when (cancellationToken.IsCancellationRequested)
        {
            Logger.Info($"Stopping of '{check.Id}' in '{check.Category}' cancelled.", e);
        }
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

                    var message = messageFactory(customCheckHostInfo, check, result);

                    await SendAndWarnOnFailure(messageSender, message, ttl, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
                {
                    Logger.Error($"Custom check '{check.Id}' in '{check.Category}' failed but can be retried.", ex);
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
                Logger.Debug($"Operation canceled while stopping custom check '{check.Id}' in '{check.Category}'.", ex);
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Custom check '{check.Id}' failed and cannot be retried.", ex);
                break;
            }
        }
    }

    async Task SendAndWarnOnFailure(ServiceControlBackend sender, ReportCustomCheckResult message, TimeSpan ttl, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(message, ttl, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            Logger.Warn($"Failed to send periodic check for '{check.Id}' in '{check.Category}' to ServiceControl.", ex);
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
            var reason = $"'{check.GetType()}' implementation for custom check '{check.Id}' in '{check.Category}' failed to run.";
            Logger.Error(reason, ex);
            return CheckResult.Failed(reason);
        }
    }
}