namespace NServiceBus.CustomChecks.AcceptanceTests;

using System.Threading.Tasks;
using AcceptanceTesting;
using NServiceBus;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

class When_no_custom_checks_exist : NServiceBusAcceptanceTest
{
    [Test]
    public async Task The_endpoint_should_start_normally() =>
        await Scenario.Define<Context>()
            .WithEndpoint<Sender>(b => b.When(ms => ms.SendLocal(new MyMessage())))
            .Done(c => c.HandlerCalled)
            .Run();

    class Context : ScenarioContext
    {
        public bool HandlerCalled { get; set; }
    }

    public class MyMessage : ICommand;

    class Sender : EndpointConfigurationBuilder
    {
        public Sender() => EndpointSetup<DefaultServer>();

        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.HandlerCalled = true;
                return Task.CompletedTask;
            }
        }
    }
}