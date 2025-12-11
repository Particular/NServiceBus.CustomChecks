namespace NServiceBus.CustomChecks
{
    using System;
    using Features;
    using Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using Transport;

    class CustomChecksFeature : Feature
    {
        public CustomChecksFeature()
        {
            Defaults(s => s.SetDefault(new CustomChecksRegistry()));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var registry = context.Settings.Get<CustomChecksRegistry>();

            registry.AddScannedTypes(context.Settings.GetAvailableTypes());

            context.Settings.TryGet("NServiceBus.CustomChecks.Ttl", out TimeSpan? ttl);

            var serviceControlQueue = context.Settings.Get<string>("NServiceBus.CustomChecks.Queue");

            context.RegisterStartupTask(b =>
            {
                var wrappers = registry.ResolveWrappers(b);

                // ReceiveAddresses is not registered on send-only endpoints
                var backend = new ServiceControlBackend(serviceControlQueue, b.GetService<ReceiveAddresses>());

                return new CustomChecksStartup(
                    wrappers,
                    b.GetRequiredService<IMessageDispatcher>(),
                    backend,
                    b.GetRequiredService<HostInformation>(),
                    context.Settings.EndpointName(),
                    ttl);
            });
        }
    }
}
