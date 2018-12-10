namespace NServiceBus.CustomChecks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class AsyncTimer
    {
        public void Start(Func<Task> callback, TimeSpan? interval, Action<Exception> errorCallback)
        {
            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            task = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await callback().ConfigureAwait(false);

                        if (interval.HasValue)
                        {
                            await Task.Delay(interval.Value, token).ConfigureAwait(false);
                        }
                        else
                        {
                            tokenSource.Cancel();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // nop	 
                    }
                    catch (Exception ex)
                    {
                        errorCallback(ex);
                    }
                }
            }, CancellationToken.None);
        }

        public Task Stop()
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
            return task;
        }

        Task task;
        CancellationTokenSource tokenSource;
    }
}