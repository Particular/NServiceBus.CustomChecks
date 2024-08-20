namespace NServiceBus.CustomChecks.AcceptanceTests
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Plugin.CustomChecks.Messages;

    public class When_using_send_only_endpoint : NServiceBusAcceptanceTest
    {
        static string ServiceControlQueue => AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(FakeServiceControl));

        [Test]
        public async Task Should_not_include_reply_to_header()
        {
            var testContext = await Scenario.Define<Context>()
                .WithEndpoint<CustomCheckEndpoint>()
                .WithEndpoint<FakeServiceControl>()
                .Done(c => c.CustomCheckResult != null)
                .Run();

            Assert.Multiple(() =>
            {
                Assert.That(testContext.CustomCheckResult.EndpointName, Is.EqualTo(AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(CustomCheckEndpoint))));
                Assert.That(testContext.CustomCheckResult.HasFailed, Is.EqualTo(false));
                Assert.That(testContext.CustomCheckResult.CustomCheckId, Is.EqualTo("CustomCheckInSendOnlyEndpoint"));

                Assert.That(testContext.CustomCheckResultHeaders[Headers.EnclosedMessageTypes], Is.EqualTo(typeof(ReportCustomCheckResult).FullName));
                Assert.That(testContext.CustomCheckResultHeaders.ContainsKey(Headers.ReplyToAddress), Is.False);
            });
        }

        class Context : ScenarioContext
        {
            public ReportCustomCheckResult CustomCheckResult { get; set; }
            public IReadOnlyDictionary<string, string> CustomCheckResultHeaders { get; set; }
        }

        class CustomCheckEndpoint : EndpointConfigurationBuilder
        {
            public CustomCheckEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ReportCustomChecksTo(ServiceControlQueue);
                    c.SendOnly();
                });
                IncludeType<ReportCustomCheckResult>();
            }


            class TestCheck : CustomCheck
            {
                public TestCheck() : base("CustomCheckInSendOnlyEndpoint", "Tests")
                {
                }

                public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default) => Task.FromResult(CheckResult.Pass);
            }
        }

        class FakeServiceControl : EndpointConfigurationBuilder
        {
            public FakeServiceControl()
            {
                IncludeType<ReportCustomCheckResult>();
                EndpointSetup<DefaultServer>();
            }

            public class CustomCheckResultHandler : IHandleMessages<ReportCustomCheckResult>
            {
                readonly Context scenarioContext;
                public CustomCheckResultHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(ReportCustomCheckResult message, IMessageHandlerContext context)
                {
                    scenarioContext.CustomCheckResultHeaders = context.MessageHeaders;
                    scenarioContext.CustomCheckResult = message;
                    return Task.FromResult(0);
                }
            }
        }
    }
}