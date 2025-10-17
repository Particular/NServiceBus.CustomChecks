namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using CustomChecks;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Plugin extension methods.
    /// </summary>
    public static class CustomChecksConfigurationExtensions
    {
        /// <summary>
        /// Sets the ServiceControl queue address.
        /// </summary>
        /// <param name="config">The endpoint configuration to modify.</param>
        /// <param name="serviceControlQueue">ServiceControl queue address.</param>
        /// <param name="timeToLive">The maximum time to live for the custom check report messages. Defaults to 4 times the check interval.</param>
        public static void ReportCustomChecksTo(this EndpointConfiguration config, string serviceControlQueue, TimeSpan? timeToLive = null)
        {
            if (serviceControlQueue == null)
            {
                throw new ArgumentNullException(nameof(serviceControlQueue));
            }
            config.EnableFeature<CustomChecksFeature>();
            config.GetSettings().Set("NServiceBus.CustomChecks.Queue", serviceControlQueue);
            if (timeToLive.HasValue)
            {
                config.GetSettings().Set("NServiceBus.CustomChecks.Ttl", timeToLive.Value);
            }
        }

        /// <summary>
        /// Adds a custom check type manually, providing an alternative to assembly scanning.
        /// </summary>
        /// <typeparam name="TCustomCheck">The custom check type to add. Must implement ICustomCheck.</typeparam>
        /// <param name="config">The endpoint configuration to extend.</param>
        /// <param name="registerOnContainer">
        /// If true, registers the type in the DI container. If false, only registers for discovery.
        /// </param>
        public static void AddCustomCheck<TCustomCheck>(this EndpointConfiguration config, bool registerOnContainer = true)
            where TCustomCheck : class, ICustomCheck
        {
            ArgumentNullException.ThrowIfNull(config);

            if (registerOnContainer)
            {
                config.RegisterComponents(c => c.AddTransient<TCustomCheck>());
            }

            config.GetSettings().GetOrCreate<CustomCheckRegistry>().AddCheck<TCustomCheck>();
        }
    }
}