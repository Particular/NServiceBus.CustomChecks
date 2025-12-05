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

        /// <summary>
        /// Sets up the CustomChecks feature using the central registry.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            // Get the registry (created by Defaults or by AddCustomCheck via GetOrCreate)
            var registry = context.Settings.Get<CustomChecksRegistry>();

            // Add assembly scanned types to the registry
            // Note: If assembly scanning is disabled (AssemblyScannerConfiguration.Disable = true),
            // GetAvailableTypes() returns an empty list, so only manually registered checks will be used.
            registry.AddScannedTypes(context.Settings.GetAvailableTypes());

            // Register all custom check types in the DI container
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
