namespace NServiceBus.CustomChecks.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting.EndpointTemplates;
    using NServiceBus.AcceptanceTesting.Support;

    public class DefaultServerWithJson : DefaultServer
    {
        public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
            base.GetConfiguration(runDescriptor, endpointConfiguration, async configuration =>
            {
                await configurationBuilderCustomization(configuration);
                configuration.UseSerialization<NewtonsoftJsonSerializer>();
            });
    }
}
