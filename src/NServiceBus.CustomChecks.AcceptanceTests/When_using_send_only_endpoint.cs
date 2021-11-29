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

            Assert.AreEqual(AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(CustomCheckEndpoint)), testContext.CustomCheckResult.EndpointName);
            Assert.AreEqual(false, testContext.CustomCheckResult.HasFailed);
            Assert.AreEqual("CustomCheckInSendOnlyEndpoint", testContext.CustomCheckResult.CustomCheckId);

            Assert.AreEqual(typeof(ReportCustomCheckResult).FullName, testContext.CustomCheckResultHeaders[Headers.EnclosedMessageTypes]);
            Assert.IsFalse(testContext.CustomCheckResultHeaders.ContainsKey(Headers.ReplyToAddress));
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