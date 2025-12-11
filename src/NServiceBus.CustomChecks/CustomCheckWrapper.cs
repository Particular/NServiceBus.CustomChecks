namespace NServiceBus.CustomChecks
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    sealed class CustomCheckWrapper<T> : ICustomCheckWrapper where T : ICustomCheck
    {
        T instance;

        public ICustomCheck Instance => instance;

        public void Initialize(IServiceProvider provider) => instance = ActivatorUtilities.CreateInstance<T>(provider);

        public async ValueTask DisposeAsync()
        {
            if (instance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

