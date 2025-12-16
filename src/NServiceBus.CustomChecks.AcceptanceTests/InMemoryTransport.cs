namespace NServiceBus.CustomChecks.AcceptanceTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Settings;
using Transport;

class InMemoryTransport : TransportDefinition
{
    public InMemoryTransport() : base(TransportTransactionMode.None, true, true, true)
    {
    }

    public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<TransportInfrastructure>(
            new InMemTransportInfrastructure(hostSettings.CoreSettings));
    }

    public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() =>
        new[] { TransportTransactionMode.None };

    class InMemTransportInfrastructure : TransportInfrastructure
    {
        Queue<TransportOperations> queue;

        public InMemTransportInfrastructure(IReadOnlySettings settings)
        {
            queue = settings.Get<Queue<TransportOperations>>("InMemQueue");
            Dispatcher = new MessageDispatcher(queue);
        }

        class MessageDispatcher : IMessageDispatcher
        {
            Queue<TransportOperations> queue;

            public MessageDispatcher(Queue<TransportOperations> queue)
            {
                this.queue = queue;
            }

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
            {
                queue.Enqueue(outgoingMessages);
                return Task.FromResult(0);
            }
        }

        public override Task Shutdown(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public override string ToTransportAddress(QueueAddress address) => address.BaseAddress;
    }
}