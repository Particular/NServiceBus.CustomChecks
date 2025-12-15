namespace NServiceBus.CustomChecks.Tests
{
    using System;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using CustomChecks;
    using NUnit.Framework;
    using Particular.Approvals;
    using ServiceControl.Plugin.CustomChecks.Messages;

    [TestFixture]
    public class ServiceControlBackendTests
    {
        [Test]
        public async Task It_can_serialize_RegisterCustomCheckResult()
        {
            var reportCustomCheckResult = new ReportCustomCheckResult
            {
                EndpointName = "My.Endpoint",
                ReportedAt = new DateTime(2016, 02, 01, 13, 59, 0, DateTimeKind.Utc),
                Host = "Host",
                HostId = Guid.Empty,
                Category = "MyCategory",
                CustomCheckId = "MyCheckId",
                FailureReason = "Failure reason.",
                HasFailed = true
            };

            using var bufferWriter = new ArrayPoolBufferWriter<byte>();
            var writer = new Utf8JsonWriter(bufferWriter);
            await using var _ = writer;
            JsonSerializer.Serialize(writer, reportCustomCheckResult, MessagesJsonContext.Default.ReportCustomCheckResult);

            Approver.Verify(Encoding.UTF8.GetString(bufferWriter.WrittenSpan));
        }
    }
}