namespace NServiceBus.CustomChecks
{
    using System;
    using Features;
    using Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using Settings;
    using Transport;

    class CustomChecksFeature : Feature
    {
        /// <summary>
        /// Sets up the CustomChecks feature using the central registry.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            // Get or create the central registry
            var registry = GetOrCreateRegistry(context.Settings);

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

        /// <summary>
        /// Gets the central CustomCheckRegistry instance from Settings.
        /// </summary>
        /// <param name="settings">The NServiceBus settings containing the registry.</param>
        /// <returns>The existing CustomCheckRegistry instance, or a new one if none exists.</returns>
        static CustomCheckRegistry GetOrCreateRegistry(IReadOnlySettings settings)
        {
            // Try to get existing registry from settings
            if (settings.TryGet<CustomCheckRegistry>("NServiceBus.CustomChecks.Registry", out var registry))
            {
                return registry;
            }

            // Create new registry if none exists
            return new CustomCheckRegistry();
        }
    }
}
