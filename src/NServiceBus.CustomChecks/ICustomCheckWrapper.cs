namespace NServiceBus.CustomChecks
{
    using System;

    interface ICustomCheckWrapper : IAsyncDisposable, IEquatable<ICustomCheckWrapper>
    {
        ICustomCheck Instance { get; }

        Type CheckType { get; }

        void Initialize(IServiceProvider provider);
    }
}

