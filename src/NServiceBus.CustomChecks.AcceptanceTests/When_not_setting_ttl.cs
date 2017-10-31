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

    public class When_not_setting_ttl : NServiceBusAcceptanceTest
    {
        static string DetectorAddress => Conventions.EndpointNamingConvention(typeof(Sender)) + ".Detector";

        [Test]
        public void Should_use_four_times_the_interval_value_for_ttl()
        {
            var result = Scenario.Define<Context>()
                .WithEndpoint<Sender>()
                .Done(c => c.DetectedMessage != null)
                .Run();

            Assert.AreEqual(TimeSpan.FromSeconds(4), result.DetectedMessage.TimeToBeReceived);
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
                    c.ReportCustomChecksTo(DetectorAddress);
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
                    : base("SuccessfulCustomCheck", "CustomCheck", TimeSpan.FromSeconds(1))
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