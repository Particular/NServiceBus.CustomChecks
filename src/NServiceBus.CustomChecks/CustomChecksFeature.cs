namespace NServiceBus.CustomChecks
{
    using System;
    using System.Linq;
    using Features;
    using Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using Transport;

    class CustomChecksFeature : Feature
    {
        /// <summary>Called when the features is activated.</summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.GetAvailableTypes()
                .Where(t => typeof(ICustomCheck).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                .ToList()
                .ForEach(t => context.Services.AddTransient(typeof(ICustomCheck), t));

            var replyToAddress = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly")
                ? context.Settings.LocalAddress()
                : null;

            context.Settings.TryGet("NServiceBus.CustomChecks.Ttl", out TimeSpan? ttl);

            var serviceControlQueue = context.Settings.Get<string>("NServiceBus.CustomChecks.Queue");
            var backend = new ServiceControlBackend(serviceControlQueue, replyToAddress);

            context.RegisterStartupTask(b => new CustomChecksStartup(b.GetServices<ICustomCheck>(), b.GetRequiredService<IMessageDispatcher>(), backend, b.GetRequiredService<HostInformation>(), context.Settings.EndpointName(), ttl));
        }
    }
}
