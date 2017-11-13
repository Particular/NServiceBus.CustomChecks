namespace NServiceBus.CustomChecks.Tests
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;
    using CustomChecks;
    using NUnit.Framework;
    using ServiceControl.Plugin.CustomChecks.Messages;

    [TestFixture]
    public class ServiceControlBackendTests
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
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
            TestApprover.Verify(Encoding.UTF8.GetString(body));
        }
    }
}