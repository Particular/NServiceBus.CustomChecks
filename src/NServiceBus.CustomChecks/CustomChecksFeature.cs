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
            Defaults(s => s.GetOrCreate<CustomCheckRegistry>());
        }

        /// <summary>
        /// Sets up the CustomChecks feature using the central registry.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            // Get the registry (created by Defaults or by AddCustomCheck via GetOrCreate)
            var registry = context.Settings.Get<CustomCheckRegistry>();

            // Add assembly scanned types to the registry
            registry.AddScannedTypes(context.Settings.GetAvailableTypes());

            // Register all custom check types in the DI container
            foreach (var type in registry.GetAllCheckTypes())
            {
                context.Services.AddTransient(typeof(ICustomCheck), type);
            }

            // Configure custom check execution settings
            context.Settings.TryGet("NServiceBus.CustomChecks.Ttl", out TimeSpan? ttl);
            var serviceControlQueue = context.Settings.Get<string>("NServiceBus.CustomChecks.Queue");

            // Register the startup task that will execute all discovered custom checks
            context.RegisterStartupTask(b =>
            {
                // Create the backend for reporting check results to ServiceControl
                var backend = new ServiceControlBackend(serviceControlQueue, b.GetService<ReceiveAddresses>());

                // Create the startup task that will execute all custom checks
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
