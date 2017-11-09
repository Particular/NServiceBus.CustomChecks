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
        /// <param name="config"></param>
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
    }
}