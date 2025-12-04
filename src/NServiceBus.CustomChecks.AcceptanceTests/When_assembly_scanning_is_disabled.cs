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

    /// <summary>
    /// Verifies that only manually registered custom checks are discovered when no checks are defined in scannable locations.
    /// This simulates the serverless/multi-endpoint scenario where checks are only registered manually.
    /// </summary>
    public class When_assembly_scanning_is_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_only_discover_manually_registered_checks()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<FakeServiceControl>()
                .WithEndpoint<Sender>()
                .Done(c => c.WasCalled)
                .Run();

            Assert.Multiple(() =>
            {
                Assert.That(context.WasCalled, Is.True);
                Assert.That(context.CustomCheckId, Is.EqualTo("ManuallyRegisteredCheck"));
                Assert.That(context.Category, Is.EqualTo("Manual"));
                // Verify only the manually registered check was discovered (no other checks in scannable locations)
                Assert.That(context.ReceivedCheckIds.Count, Is.EqualTo(1));
                Assert.That(context.ReceivedCheckIds, Contains.Item("ManuallyRegisteredCheck"));
            });
        }

        class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public string CustomCheckId { get; set; }
            public string Category { get; set; }
            public List<string> ReceivedCheckIds { get; } = [];
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var receiverEndpoint = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(FakeServiceControl));

                    c.ReportCustomChecksTo(receiverEndpoint);

                    // Register custom check manually - this should be the only one discovered
                    // because ManuallyRegisteredCheck is defined outside the endpoint class
                    // and won't be discovered by scanning
                    c.AddCustomCheck<ManuallyRegisteredCheck>();
                });
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
                    testContext.CustomCheckId = message.CustomCheckId;
                    testContext.Category = message.Category;
                    testContext.ReceivedCheckIds.Add(message.CustomCheckId);
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }
    }

    // This check is defined outside the endpoint class to ensure it's not discovered by scanning
    // It will only be registered via the manual registration API
    class ManuallyRegisteredCheck : CustomCheck
    {
        public ManuallyRegisteredCheck()
            : base("ManuallyRegisteredCheck", "Manual")
        {
        }

        public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
        {
            return CheckResult.Pass;
        }
    }
}

