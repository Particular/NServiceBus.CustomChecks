namespace NServiceBus.CustomChecks;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Performance.TimeToBeReceived;
using Routing;
using ServiceControl.Plugin.CustomChecks.Messages;
using Transport;

sealed class ServiceControlBackend
{
    public ServiceControlBackend(string destinationQueue, ReceiveAddresses? receiveAddresses)
    {
        destinationAddressTag = new UnicastAddressTag(destinationQueue);
        headers = new Dictionary<string, string>
        {
            [Headers.EnclosedMessageTypes] = typeof(ReportCustomCheckResult).FullName!,
            [Headers.ContentType] = ContentTypes.Json,
            [Headers.MessageIntent] = SendIntent
        };
        if (receiveAddresses != null)
        {
            headers[Headers.ReplyToAddress] = receiveAddresses.MainReceiveAddress;
        }
    }

    public async Task Send(ReportCustomCheckResult messageToSend, TimeSpan timeToBeReceived, CancellationToken cancellationToken = default)
    {
        using var bufferWriter = new ArrayPoolBufferWriter<byte>();
        var writer = new Utf8JsonWriter(bufferWriter);
        await using var _ = writer.ConfigureAwait(false);
        JsonSerializer.Serialize(writer, messageToSend, MessagesJsonContext.Default.ReportCustomCheckResult);
        await Send(bufferWriter.WrittenMemory, timeToBeReceived, cancellationToken).ConfigureAwait(false);
    }

    [MemberNotNull(nameof(messageSender))]
    public void Start(IMessageDispatcher dispatcher) => messageSender = dispatcher;

    Task Send(ReadOnlyMemory<byte> body, TimeSpan timeToBeReceived, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageSender);

        var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
        var dispatchProperties = new DispatchProperties
        {
            DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived)
        };
        var operation = new TransportOperation(outgoingMessage, destinationAddressTag, dispatchProperties);
        return messageSender.Dispatch(new TransportOperations(operation), new TransportTransaction(), cancellationToken);
    }

    const string SendIntent = nameof(MessageIntent.Send);
    readonly UnicastAddressTag destinationAddressTag;
    IMessageDispatcher? messageSender;
    readonly Dictionary<string, string> headers;
}