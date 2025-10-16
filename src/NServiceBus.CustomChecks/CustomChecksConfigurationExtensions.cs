namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using CustomChecks;

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
        /// This method allows explicit registration of custom checks, enabling serverless scenarios
        /// and ahead-of-time (AOT) compilation support.
        /// 
        /// The custom check will be discovered by CustomChecksFeature alongside any checks
        /// found via assembly scanning (hybrid mode).
        /// </summary>
        /// <typeparam name="TCustomCheck">The custom check type to add. Must implement ICustomCheck.</typeparam>
        /// <param name="config">The endpoint configuration to extend.</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
        /// <exception cref="ArgumentException">Thrown if TCustomCheck does not implement ICustomCheck.</exception>
        /// <example>
        /// <code>
        /// var config = new EndpointConfiguration("MyEndpoint");
        /// config.AddCustomCheck&lt;DatabaseHealthCheck&gt;();
        /// config.ReportCustomChecksTo("ServiceControl@MyMachine");
        /// </code>
        /// </example>
        public static void AddCustomCheck<TCustomCheck>(this EndpointConfiguration config)
            where TCustomCheck : class, ICustomCheck
        {
            ArgumentNullException.ThrowIfNull(config);

            // Get or create the registry that stores manually registered custom checks
            var registry = GetOrCreateRegistry(config);

            // Add the custom check type to the registry
            // This will be picked up by CustomChecksFeature during setup
            registry.AddCheck<TCustomCheck>();
        }

        /// <summary>
        /// Adds a custom check type manually, providing an alternative to assembly scanning.
        /// This method allows explicit registration of custom checks, enabling serverless scenarios
        /// and ahead-of-time (AOT) compilation support.
        /// 
        /// The custom check will be discovered by CustomChecksFeature alongside any checks
        /// found via assembly scanning (hybrid mode).
        /// </summary>
        /// <param name="config">The endpoint configuration to extend.</param>
        /// <param name="checkType">The custom check type to add. Must implement ICustomCheck.</param>
        /// <exception cref="ArgumentNullException">Thrown if config or checkType is null.</exception>
        /// <exception cref="ArgumentException">Thrown if checkType does not implement ICustomCheck.</exception>
        /// <example>
        /// <code>
        /// var config = new EndpointConfiguration("MyEndpoint");
        /// config.AddCustomCheck(typeof(DatabaseHealthCheck));
        /// config.ReportCustomChecksTo("ServiceControl@MyMachine");
        /// </code>
        /// </example>
        public static void AddCustomCheck(this EndpointConfiguration config, Type checkType)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(checkType);

            // Get or create the registry that stores manually registered custom checks
            var registry = GetOrCreateRegistry(config);

            // Add the custom check type to the registry
            // This will be picked up by CustomChecksFeature during setup
            registry.AddCheck(checkType);
        }

        /// <summary>
        /// Gets or creates the CustomCheckRegistry instance stored in the endpoint configuration settings.
        /// The registry is stored using the key "NServiceBus.CustomChecks.Registry" and is used
        /// by CustomChecksFeature to support hybrid mode (manual registration + assembly scanning).
        /// </summary>
        /// <param name="config">The endpoint configuration containing the settings.</param>
        /// <returns>The CustomCheckRegistry instance, creating it if it doesn't exist.</returns>
        static CustomCheckRegistry GetOrCreateRegistry(EndpointConfiguration config)
        {
            // Try to get existing registry from settings
            if (!config.GetSettings().TryGet<CustomCheckRegistry>("NServiceBus.CustomChecks.Registry", out var registry))
            {
                // Create new registry if none exists
                registry = new CustomCheckRegistry();

                // Store it in settings so CustomChecksFeature can find it later
                config.GetSettings().Set("NServiceBus.CustomChecks.Registry", registry);
            }
            return registry;
        }
    }
}