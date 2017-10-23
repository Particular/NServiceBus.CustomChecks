namespace NServiceBus.CustomChecks.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using CustomChecks;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    class When_servicecontrol_queue_is_invalid : NServiceBusAcceptanceTest
    {
        [Test]
        public void The_endpoint_should_not_start_with_custom_checks()
        {
            var ex = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<Context>()
                .WithEndpoint<Sender>()
                .Run());

            Assert.IsTrue(ex.Message.Contains("You have enabled custom checks in your endpoint, however, this endpoint is unable to contact the ServiceControl to report endpoint information."));
        }

        class Context : ScenarioContext
        {
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c => c.ReportCustomChecksTo(new string(Path.GetInvalidPathChars())));
            }
        }

        class MyCustomCheck : CustomCheck
        {
            public MyCustomCheck()
                : base("SuccessfulCustomCheck", "CustomCheck")
            {
            }

            public override Task<CheckResult> PerformCheck()
            {
                return CheckResult.Pass;
            }
        }
    }
}