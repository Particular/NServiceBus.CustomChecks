namespace NServiceBus.CustomChecks.AcceptanceTests;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using CustomChecks;
using NServiceBus;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;
using ServiceControl.Plugin.CustomChecks.Messages;

public class When_registering_custom_check_dispoable : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_dispose_them()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FakeServiceControl>()
            .WithEndpoint<Sender>()
            .Done(c => c.Checks.Contains("DisposableCheck") && c.Checks.Contains("AsyncDisposableCheck"))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.WasDisposed, Is.True);
            Assert.That(context.WasAsyncDisposed, Is.True);
        }
    }

    class Context : ScenarioContext
    {
        public bool WasDisposed { get; set; }
        public bool WasAsyncDisposed { get; set; }
        public ConcurrentBag<string> Checks { get; set; } = [];
    }

    class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(c =>
            {
                var receiverEndpoint = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(FakeServiceControl));

                c.AddCustomCheck<AsyncDisposableCheck>();
                c.AddCustomCheck<DisposableCheck>();

                c.ReportCustomChecksTo(receiverEndpoint);
            });

        sealed class AsyncDisposableCheck(Context testContext) : CustomCheck("AsyncDisposableCheck", "Disposables"), IAsyncDisposable
        {
            public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default) => CheckResult.Pass;

            public ValueTask DisposeAsync()
            {
                testContext.WasAsyncDisposed = true;
                return ValueTask.CompletedTask;
            }
        }

        sealed class DisposableCheck(Context testContext) : CustomCheck("DisposableCheck", "Disposables"), IDisposable
        {
            public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default) => CheckResult.Pass;

            public void Dispose() => testContext.WasDisposed = true;
        }
    }

    class FakeServiceControl : EndpointConfigurationBuilder
    {
        public FakeServiceControl()
        {
            IncludeType<ReportCustomCheckResult>();
            EndpointSetup<DefaultServer>();
        }

        public class MyMessageHandler(Context testContext) : IHandleMessages<ReportCustomCheckResult>
        {
            public Task Handle(ReportCustomCheckResult message, IMessageHandlerContext context)
            {
                testContext.Checks.Add(message.CustomCheckId);
                return Task.CompletedTask;
            }
        }
    }
}