namespace NServiceBus.CustomChecks;

using System;

interface ICustomCheckWrapper : ICustomCheck, IAsyncDisposable, IEquatable<ICustomCheckWrapper>
{
    Type CheckType { get; }
    void Initialize(IServiceProvider provider);
}