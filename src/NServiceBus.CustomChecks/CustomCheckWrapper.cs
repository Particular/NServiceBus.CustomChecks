#nullable enable

namespace NServiceBus.CustomChecks;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

sealed class CustomCheckWrapper<T> : ICustomCheckWrapper
    where T : ICustomCheck
{
    T? instance;

    public Type CheckType => typeof(T);

    public string Category
    {
        get
        {
            ArgumentNullException.ThrowIfNull(instance);
            return instance.Category;
        }
    }

    public string Id
    {
        get
        {
            ArgumentNullException.ThrowIfNull(instance);
            return instance.Id;
        }
    }

    public TimeSpan? Interval
    {
        get
        {
            ArgumentNullException.ThrowIfNull(instance);
            return instance.Interval;
        }
    }

    [MemberNotNull(nameof(instance))]
    public void Initialize(IServiceProvider provider) => instance = ActivatorUtilities.CreateInstance<T>(provider);

    [DebuggerNonUserCode]
    public Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return instance.PerformCheck(cancellationToken);
    }

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