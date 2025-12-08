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
            // Ensure registry exists (created here for scanning-only scenarios, or already exists from manual registration)
            Defaults(s => s.GetOrCreate<CustomChecksRegistry>());
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var registry = context.Settings.Get<CustomChecksRegistry>();

            registry.AddScannedTypes(context.Settings.GetAvailableTypes());

            foreach (var type in registry.GetAllCheckTypes())
            {
                context.Services.AddTransient(typeof(ICustomCheck), type);
            }

            context.Settings.TryGet("NServiceBus.CustomChecks.Ttl", out TimeSpan? ttl);

            var serviceControlQueue = context.Settings.Get<string>("NServiceBus.CustomChecks.Queue");

            context.RegisterStartupTask(b =>
            {
                // ReceiveAddresses is not registered on send-only endpoints
                var backend = new ServiceControlBackend(serviceControlQueue, b.GetService<ReceiveAddresses>());

                return new CustomChecksStartup(
                    b.GetServices<ICustomCheck>(),
                    b.GetRequiredService<IMessageDispatcher>(),
                    backend,
                    b.GetRequiredService<HostInformation>(),
                    context.Settings.EndpointName(),
                    ttl);
            });
        }
    }
}
