namespace NServiceBus.CustomChecks.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using CustomChecks;
    using NUnit.Framework;
    using Transport;

    public class When_using_one_time_check : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_infinite_ttl_even_when_explicitly_configured()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(c => c.CustomConfig((cfg, ctx) => cfg.GetSettings().Set("InMemQueue", ctx.Queue)))
                .Done(c => c.Queue.Count > 0)
                .Run();

            var message = context.Queue.Dequeue();

            var constraint = message.UnicastTransportOperations.First().Properties.DiscardIfNotReceivedBefore;
            Assert.AreEqual(TimeSpan.MaxValue, constraint.MaxTime);
        }

        class Context : ScenarioContext
        {
            public Queue<TransportOperations> Queue { get; } = new Queue<TransportOperations>();
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServerWithJson>(c =>
                {
                    c.ReportCustomChecksTo("ServiceControl", TimeSpan.FromSeconds(6));
                    c.UseTransport(new InMemoryTransport());
                    c.SendOnly();
                });
            }

            class FailingCustomCheck : CustomCheck
            {
                public FailingCustomCheck()
                    : base("SuccessfulCustomCheck", "CustomCheck")
                {
                }

                public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
                {
                    return CheckResult.Pass;
                }
            }
        }
    }
}