namespace NServiceBus.CustomChecks.Tests
{
    using System;
    using System.Text;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using CustomChecks;
    using NUnit.Framework;
    using ServiceControl.Plugin.CustomChecks.Messages;

    [TestFixture]
    [UseReporter(typeof(DiffReporter))]
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
            Approvals.Verify(Encoding.UTF8.GetString(body));
        }
    }
}