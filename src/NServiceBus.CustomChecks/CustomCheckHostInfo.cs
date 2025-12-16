namespace NServiceBus.CustomChecks;

using System;

record CustomCheckHostInfo(string EndpointName, string HostDisplayName, Guid HostId);
