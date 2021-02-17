namespace NServiceBus.CustomChecks
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Performance.TimeToBeReceived;
    using Routing;
    using SimpleJson;
    using Transport;

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

        public void Start(IMessageDispatcher dispatcher)
        {
            messageSender = dispatcher;
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
            var dispatchProperties = new DispatchProperties
            {
                DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived)
            };
            var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(destinationQueue), dispatchProperties);
            return messageSender?.Dispatch(new TransportOperations(operation), new TransportTransaction());
        }

        static string sendIntent = MessageIntentEnum.Send.ToString();
        string destinationQueue;
        string localAddress;

        static IJsonSerializerStrategy serializerStrategy = new MessageSerializationStrategy();
        IMessageDispatcher messageSender;
    }
}