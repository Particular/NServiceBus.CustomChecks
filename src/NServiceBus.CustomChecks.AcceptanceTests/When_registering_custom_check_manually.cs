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

    public class When_registering_custom_check_manually : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_result_to_service_control()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<FakeServiceControl>()
                .WithEndpoint<Sender>()
                .Done(c => c.WasCalled)
                .Run();

            Assert.Multiple(() =>
            {
                Assert.That(context.WasCalled, Is.True);
                Assert.That(context.FailureReason, Is.Null);
                Assert.That(context.CustomCheckId, Is.EqualTo("ManualOnlyCheck"));
                Assert.That(context.Category, Is.EqualTo("Manual"));
                Assert.That(context.ReportedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(3.0)));
                Assert.That(context.Headers.ContainsKey(Headers.ReplyToAddress), Is.True);
            });
        }

        class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public string FailureReason { get; set; }
            public string CustomCheckId { get; set; }
            public string Category { get; set; }
            public DateTime ReportedAt { get; set; }
            public IReadOnlyDictionary<string, string> Headers { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var receiverEndpoint = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(FakeServiceControl));

                    c.ReportCustomChecksTo(receiverEndpoint);

                    // Register custom check manually - this proves manual registration works
                    c.AddCustomCheck<ManualOnlyCheck>();
                });
            }
        }

        class FakeServiceControl : EndpointConfigurationBuilder
        {
            public FakeServiceControl()
            {
                IncludeType<ReportCustomCheckResult>();

                // todo: mm review
                ExcludeType<ManualOnlyCheck>();

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
                    testContext.Headers = context.MessageHeaders;
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }
    }

    // This check is defined outside the endpoint class to ensure it's not discovered by scanning
    // It will only be registered via the manual registration API
    class ManualOnlyCheck : CustomCheck
    {
        //todo: not necessary - could move inside.... 

        public ManualOnlyCheck()
            : base("ManualOnlyCheck", "Manual")
        {
        }

        public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
        {
            return CheckResult.Pass;
        }
    }
}
