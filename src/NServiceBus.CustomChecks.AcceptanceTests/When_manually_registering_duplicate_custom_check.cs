namespace NServiceBus.CustomChecks.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using CustomChecks;
    using NServiceBus;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Plugin.CustomChecks.Messages;

    public class When_manually_registering_duplicate_custom_check : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deduplicate_and_execute_only_once()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<FakeServiceControl>()
                .WithEndpoint<Sender>()
                .Done(c => c.ReceivedCheckIds.Count >= 1)
                .Run();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.ReceivedCheckIds.Count, Is.EqualTo(1));
                Assert.That(context.ReceivedCheckIds, Contains.Item("DuplicateCustomCheck"));
            }
        }

        class Context : ScenarioContext
        {
            public List<string> ReceivedCheckIds { get; } = [];
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    var receiverEndpoint = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(FakeServiceControl));

                    c.ReportCustomChecksTo(receiverEndpoint);

                    c.AddCustomCheck<OurCustomCheck>();
                    c.AddCustomCheck<OurCustomCheck>();

                })
                .IncludeType<OurCustomCheck>();

            class OurCustomCheck() : CustomCheck("DuplicateCustomCheck", "DuplicateCheck")
            {
                public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default) => CheckResult.Pass;
            }
        }

        class FakeServiceControl : EndpointConfigurationBuilder
        {
            public FakeServiceControl()
            {
                IncludeType<ReportCustomCheckResult>();
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler(Context testContext) : IHandleMessages<ReportCustomCheckResult>
            {
                public Task Handle(ReportCustomCheckResult message, IMessageHandlerContext context)
                {
                    testContext.ReceivedCheckIds.Add(message.CustomCheckId);
                    return Task.CompletedTask;
                }
            }
        }
    }
}

