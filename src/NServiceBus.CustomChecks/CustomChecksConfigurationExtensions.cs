namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
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
        public static void ReportCustomChecksTo(this EndpointConfiguration config, string serviceControlQueue)
        {
            if (serviceControlQueue == null)
            {
                throw new ArgumentNullException(nameof(serviceControlQueue));
            }
            config.EnableFeature<CustomChecksFeature>();
            config.GetSettings().Set("NServiceBus.CustomChecks.Queue", serviceControlQueue);
        }
    }
}