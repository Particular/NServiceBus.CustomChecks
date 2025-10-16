namespace NServiceBus.CustomChecks.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using CustomChecks;
    using NServiceBus;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Plugin.CustomChecks.Messages;

    public class When_registering_custom_checks_in_hybrid_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_results_from_both_manually_registered_and_scanned_checks()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<FakeServiceControl>()
                .WithEndpoint<Sender>()
                .Done(c => c.ReceivedCheckIds.Count >= 2)
                .Run();

            Assert.Multiple(() =>
            {
                Assert.That(context.ReceivedCheckIds.Count, Is.EqualTo(2));
                Assert.That(context.ReceivedCheckIds, Contains.Item("HybridManualCheck"));
                Assert.That(context.ReceivedCheckIds, Contains.Item("ScannedCustomCheck"));
                Assert.That(context.FailureReasons.All(f => f == null), Is.True);
            });
        }

        class Context : ScenarioContext
        {
            public List<string> ReceivedCheckIds { get; } = [];
            public List<string> FailureReasons { get; } = [];
            public List<string> Categories { get; } = [];
            public List<DateTime> ReportedAts { get; } = [];
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var receiverEndpoint = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(FakeServiceControl));

                    c.ReportCustomChecksTo(receiverEndpoint);

                    // Register one check manually - this proves manual registration works
                    c.AddCustomCheck<HybridManualCheck>();

                    // The ScannedCustomCheck below will be discovered via assembly scanning
                    // This proves hybrid mode works (both manual + scanning)
                });
            }

            // This check will be discovered by assembly scanning
            class ScannedCustomCheck : CustomCheck
            {
                public ScannedCustomCheck()
                    : base("ScannedCustomCheck", "Scanned")
                {
                }

                public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
                {
                    return CheckResult.Pass;
                }
            }
        }

        class FakeServiceControl : EndpointConfigurationBuilder
        {
            public FakeServiceControl()
            {
                IncludeType<ReportCustomCheckResult>();
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<ReportCustomCheckResult>
            {
                Context testContext;

                public MyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(ReportCustomCheckResult message, IMessageHandlerContext context)
                {
                    testContext.ReceivedCheckIds.Add(message.CustomCheckId);
                    testContext.FailureReasons.Add(message.FailureReason);
                    testContext.Categories.Add(message.Category);
                    testContext.ReportedAts.Add(message.ReportedAt);
                    return Task.FromResult(0);
                }
            }
        }
    }

    // This check is defined outside the endpoint class to ensure it's not discovered by scanning
    // It will only be registered via the manual registration API
    class HybridManualCheck : CustomCheck
    {
        public HybridManualCheck()
            : base("HybridManualCheck", "Hybrid")
        {
        }

        public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
        {
            return CheckResult.Pass;
        }
    }
}
