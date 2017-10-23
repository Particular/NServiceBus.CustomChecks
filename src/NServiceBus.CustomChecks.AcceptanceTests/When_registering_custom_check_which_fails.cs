namespace NServiceBus.CustomChecks.AcceptanceTests
{
    using System;
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
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<FakeServiceControl>()
                .WithEndpoint<Sender>()
                .Done(c => c.WasCalled)
                .Run(TimeSpan.FromSeconds(10));

            Assert.True(context.WasCalled);
            Assert.AreEqual("Some reason", context.FailureReason);
            Assert.AreEqual("FailingCustomCheck", context.CustomCheckId);
            Assert.AreEqual("CustomCheck", context.Category);
            Assert.That(context.ReportedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(3.0)));
        }

        class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }

            public Guid Id { get; set; }

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
                    var receiverEndpoint = NServiceBus.AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(FakeServiceControl));
                    c.ReportCustomChecksTo(receiverEndpoint);
                });
            }

            class FailingCustomCheck : CustomCheck
            {
                public FailingCustomCheck()
                    : base("FailingCustomCheck", "CustomCheck")
                {
                }

                public override Task<CheckResult> PerformCheck()
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
                public Context TestContext { get; set; }

                public Task Handle(ReportCustomCheckResult message, IMessageHandlerContext context)
                {
                    TestContext.FailureReason = message.FailureReason;
                    TestContext.CustomCheckId = message.CustomCheckId;
                    TestContext.Category = message.Category;
                    TestContext.ReportedAt = message.ReportedAt;
                    TestContext.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }
    }
}