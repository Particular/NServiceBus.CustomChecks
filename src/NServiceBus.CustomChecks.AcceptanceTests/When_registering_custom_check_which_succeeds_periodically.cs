namespace NServiceBus.CustomChecks.AcceptanceTests
{
    using System;
    using System.Threading;
    using AcceptanceTesting;
    using CustomChecks;
    using NServiceBus;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Plugin.CustomChecks.Messages;

    public class When_registering_custom_check_which_succeeds_periodically : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_send_result_to_service_control()
        {
            var context = Scenario.Define<Context>()
                .WithEndpoint<FakeServiceControl>()
                .WithEndpoint<Sender>()
                .Done(c => c.Times >= 2)
                .Run();

            Assert.Null(context.FailureReason);
            Assert.AreEqual("SuccessfulCustomCheck", context.CustomCheckId);
            Assert.AreEqual("CustomCheck", context.Category);
            Assert.That(context.ReportedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(3.0)));
        }

        class Context : ScenarioContext
        {
            long times;

            public long Times => Interlocked.Read(ref times);

            public string FailureReason { get; set; }
            public string CustomCheckId { get; set; }
            public string Category { get; set; }
            public DateTime ReportedAt { get; set; }

            public void Called()
            {
                times = Interlocked.Increment(ref times);
            }
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
                    : base("SuccessfulCustomCheck", "CustomCheck", TimeSpan.FromSeconds(1))
                {
                }

                public override CheckResult PerformCheck()
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
                public Context TestContext { get; set; }

                public void Handle(ReportCustomCheckResult message)
                {
                    TestContext.FailureReason = message.FailureReason;
                    TestContext.CustomCheckId = message.CustomCheckId;
                    TestContext.Category = message.Category;
                    TestContext.ReportedAt = message.ReportedAt;
                    TestContext.Called();
                }
            }
        }
    }
}