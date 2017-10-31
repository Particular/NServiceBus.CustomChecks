namespace NServiceBus.CustomChecks
{
    using System.Linq;
    using Features;
    using NServiceBus;

    class CustomChecksFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.GetAvailableTypes()
                .Where(t => typeof(ICustomCheck).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                .ToList()
                .ForEach(t => context.Container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));
        }
    }
}
