namespace NServiceBus.CustomChecks
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Extensibility;
    using Performance.TimeToBeReceived;
    using Routing;
    using SimpleJson;
    using Transport;
    using Unicast.Transport;

    class ServiceControlBackend
    {
        public ServiceControlBackend(string destinationQueue, string localAddress)
        {
            this.destinationQueue = destinationQueue;
            this.localAddress = localAddress;
        }

        public Task Send(object messageToSend, TimeSpan timeToBeReceived)
        {
            var body = Serialize(messageToSend);
            return Send(body, messageToSend.GetType().FullName, timeToBeReceived);
        }

        internal static byte[] Serialize(object messageToSend)
        {
            return Encoding.UTF8.GetBytes(SimpleJson.SerializeObject(messageToSend, serializerStrategy));
        }

        public async Task Start(IDispatchMessages dispatcher)
        {
            messageSender = dispatcher;
            try
            {
                // In order to verify if the queue exists, we are sending a control message to SC.
                // If we are unable to send a message because the queue doesn't exist, then we can fail fast.
                // We currently don't have a way to check if Queue exists in a transport agnostic way,
                // hence the send.
                var outgoingMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);
                outgoingMessage.Headers[Headers.ReplyToAddress] = localAddress;
                var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(destinationQueue));
                await messageSender.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                const string errMsg = @"You have enabled custom checks in your endpoint, however, this endpoint is unable to contact the ServiceControl to report endpoint information.
Please ensure that the specified queue is correct.";

                throw new Exception(errMsg, ex);
            }
        }

        Task Send(byte[] body, string messageType, TimeSpan timeToBeReceived)
        {
            var headers = new Dictionary<string, string>
            {
                [Headers.EnclosedMessageTypes] = messageType,
                [Headers.ContentType] = ContentTypes.Json,
                [Headers.MessageIntent] = sendIntent
            };
            if (localAddress != null)
            {
                headers[Headers.ReplyToAddress] = localAddress;
            }

            var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
            var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(destinationQueue), deliveryConstraints: new List<DeliveryConstraint>
            {
                new DiscardIfNotReceivedBefore(timeToBeReceived)
            });
            return messageSender?.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag());
        }

        static string sendIntent = MessageIntentEnum.Send.ToString();
        string destinationQueue;
        string localAddress;

        static IJsonSerializerStrategy serializerStrategy = new MessageSerializationStrategy();
        IDispatchMessages messageSender;
    }
}