namespace NServiceBus.CustomChecks.AcceptanceTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using CustomChecks;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;
using Transport;

public class When_not_setting_ttl : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_four_times_the_interval_value_for_ttl()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Sender>(c => c.CustomConfig((cfg, ctx) => cfg.GetSettings().Set("InMemQueue", ctx.Queue)))
            .Done(c => c.Queue.Count > 0)
            .Run();

        var message = context.Queue.Dequeue();

        var constraint = message.UnicastTransportOperations.First().Properties.DiscardIfNotReceivedBefore;
        Assert.That(constraint.MaxTime, Is.EqualTo(TimeSpan.FromSeconds(4)));
    }

    class Context : ScenarioContext
    {
        public Queue<TransportOperations> Queue { get; } = new();
    }

    class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.ReportCustomChecksTo("ServiceControl");
                c.UseTransport(new InMemoryTransport());
                c.SendOnly();
            }).IncludeType<SuccessfulCustomCheck>();

        class SuccessfulCustomCheck() : CustomCheck("SuccessfulCustomCheck", "CustomCheck", TimeSpan.FromSeconds(1))
        {
            public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default) => CheckResult.Pass;
        }
    }
}