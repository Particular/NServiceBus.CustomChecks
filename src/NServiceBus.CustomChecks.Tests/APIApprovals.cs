using NServiceBus.CustomChecks;
using NUnit.Framework;
using Particular.Approvals;
using PublicApiGenerator;

[TestFixture]
public class APIApprovals
{
    [Test]
    public void Approve()
    {
        var publicApi = ApiGenerator.GeneratePublicApi(typeof(CustomCheck).Assembly);
        Approver.Verify(publicApi);
    }
}