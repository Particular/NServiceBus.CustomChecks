#nullable enable

namespace NServiceBus.CustomChecks
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    sealed class CustomCheckWrapper<T> : ICustomCheckWrapper where T : ICustomCheck
    {
        T? instance;

        public ICustomCheck Instance => instance!;

        public Type CheckType { get; } = typeof(T);

        public void Initialize(IServiceProvider provider) => instance = ActivatorUtilities.CreateInstance<T>(provider);

        public bool Equals(ICustomCheckWrapper? other) => other?.CheckType == CheckType;

        public override int GetHashCode() => CheckType.GetHashCode();

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

