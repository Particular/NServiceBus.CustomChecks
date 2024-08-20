namespace NServiceBus.CustomChecks.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using CustomChecks;
    using NServiceBus;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Plugin.CustomChecks.Messages;

    public class When_registering_custom_check_which_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_result_to_service_control()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<FakeServiceControl>()
                .WithEndpoint<Sender>()
                .Done(c => c.WasCalled)
                .Run(TimeSpan.FromSeconds(10));

            Assert.That(context.WasCalled, Is.True);
            Assert.AreEqual("Some reason", context.FailureReason);
            Assert.AreEqual("FailingCustomCheck", context.CustomCheckId);
            Assert.AreEqual("CustomCheck", context.Category);
            Assert.That(context.ReportedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(3.0)));
        }

        class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public string FailureReason { get; set; }
            public string CustomCheckId { get; set; }
            public string Category { get; set; }
            public DateTime ReportedAt { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var receiverEndpoint = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(FakeServiceControl));
                    c.ReportCustomChecksTo(receiverEndpoint);
                });
            }

            class FailingCustomCheck : CustomCheck
            {
                public FailingCustomCheck()
                    : base("FailingCustomCheck", "CustomCheck")
                {
                }

                public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
                {
                    return CheckResult.Failed("Some reason");
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
                    testContext.FailureReason = message.FailureReason;
                    testContext.CustomCheckId = message.CustomCheckId;
                    testContext.Category = message.Category;
                    testContext.ReportedAt = message.ReportedAt;
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }
    }
}