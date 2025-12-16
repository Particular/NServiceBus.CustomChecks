namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Configuration.AdvancedExtensibility;
using CustomChecks;

/// <summary>
/// Plugin extension methods.
/// </summary>
public static class CustomChecksConfigurationExtensions
{
    /// <param name="config">The endpoint configuration to modify.</param>
    extension(EndpointConfiguration config)
    {
        /// <summary>
        /// Sets the ServiceControl queue address.
        /// </summary>
        /// <param name="serviceControlQueue">ServiceControl queue address.</param>
        /// <param name="timeToLive">The maximum time to live for the custom check report messages. Defaults to 4 times the check interval.</param>
        public void ReportCustomChecksTo(string serviceControlQueue, TimeSpan? timeToLive = null)
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
        /// Manually registers a Custom Check
        /// </summary>
        /// <typeparam name="TCustomCheck">The Custom Check type to add.</typeparam>
        public void AddCustomCheck<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCustomCheck>()
            where TCustomCheck : class, ICustomCheck
        {
            ArgumentNullException.ThrowIfNull(config);

            config.GetSettings().GetOrCreate<CustomChecksRegistry>().AddCheck<TCustomCheck>();
        }
    }
}