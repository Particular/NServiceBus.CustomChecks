namespace NServiceBus.CustomChecks;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Performance.TimeToBeReceived;
using Routing;
using Transport;

sealed class ServiceControlBackend(string destinationQueue, ReceiveAddresses? receiveAddresses)
{
    public Task Send(object messageToSend, TimeSpan timeToBeReceived, CancellationToken cancellationToken = default)
    {
        var body = Serialize(messageToSend);
        return Send(body, messageToSend.GetType().FullName!, timeToBeReceived, cancellationToken);
    }

    internal static byte[] Serialize(object messageToSend) => JsonSerializer.SerializeToUtf8Bytes(messageToSend);

    [MemberNotNull(nameof(messageSender))]
    public void Start(IMessageDispatcher dispatcher) => messageSender = dispatcher;

    Task Send(byte[] body, string messageType, TimeSpan timeToBeReceived, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageSender);

        var headers = new Dictionary<string, string>
        {
            [Headers.EnclosedMessageTypes] = messageType,
            [Headers.ContentType] = ContentTypes.Json,
            [Headers.MessageIntent] = SendIntent
        };
        if (receiveAddresses != null)
        {
            headers[Headers.ReplyToAddress] = receiveAddresses.MainReceiveAddress;
        }

        var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
        var dispatchProperties = new DispatchProperties
        {
            DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived)
        };
        var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(destinationQueue), dispatchProperties);
        return messageSender.Dispatch(new TransportOperations(operation), new TransportTransaction(), cancellationToken);
    }

    const string SendIntent = nameof(MessageIntent.Send);

    IMessageDispatcher? messageSender;
}