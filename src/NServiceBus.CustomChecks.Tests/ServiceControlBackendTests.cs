namespace NServiceBus.CustomChecks.Tests
{
    using System;
    using System.Text;
    using CustomChecks;
    using NUnit.Framework;
    using Particular.Approvals;
    using ServiceControl.Plugin.CustomChecks.Messages;

    [TestFixture]
    public class ServiceControlBackendTests
    {
        [Test]
        public void It_can_serialize_RegisterCustomCheckResult()
        {
            var body = ServiceControlBackend.Serialize(new ReportCustomCheckResult
            {
                EndpointName = "My.Endpoint",
                ReportedAt = new DateTime(2016, 02, 01, 13, 59, 0, DateTimeKind.Utc),
                Host = "Host",
                HostId = Guid.Empty,
                Category = "MyCategory",
                CustomCheckId = "MyCheckId",
                FailureReason = "Failure reason.",
                HasFailed = true
            });
            Approver.Verify(Encoding.UTF8.GetString(body));
        }
    }
}