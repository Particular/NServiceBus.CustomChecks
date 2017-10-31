namespace NServiceBus.CustomChecks.AcceptanceTests
{
    using System;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using CustomChecks;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Satellites;

    public class When_using_one_time_check : NServiceBusAcceptanceTest
    {
        static string DetectorAddress => Conventions.EndpointNamingConvention(typeof(Sender)) + ".Detector";

        [Test]
        public void Should_use_infinite_ttl_even_when_explicitly_configured()
        {
            var result = Scenario.Define<Context>()
                .WithEndpoint<Sender>()
                .Done(c => c.DetectedMessage != null)
                .Run();

            Assert.Greater(result.DetectedMessage.TimeToBeReceived, TimeSpan.FromDays(49710)); //Max TTL in MSMQ
        }

        class Context : ScenarioContext
        {
            public TransportMessage DetectedMessage { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ReportCustomChecksTo(DetectorAddress, TimeSpan.FromSeconds(6));
                });
            }

            class Detector : ISatellite
            {
                public Context Context { get; set; }

                public bool Handle(TransportMessage message)
                {
                    Context.DetectedMessage = message;
                    return true;
                }

                public void Start()
                {
                }

                public void Stop()
                {
                }

                public Address InputAddress => Address.Parse(DetectorAddress);

                public bool Disabled => false;
            }

            class FailingCustomCheck : CustomCheck
            {
                public FailingCustomCheck()
                    : base("SuccessfulCustomCheck", "CustomCheck")
                {
                }

                public override CheckResult PerformCheck()
                {
                    return CheckResult.Pass;
                }
            }
        }
    }
}