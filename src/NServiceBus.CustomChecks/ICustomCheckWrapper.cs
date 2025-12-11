namespace NServiceBus.CustomChecks
{
    using System;

    interface ICustomCheckWrapper : IAsyncDisposable
    {
        ICustomCheck Instance { get; }

        void Initialize(IServiceProvider provider);
    }
}

