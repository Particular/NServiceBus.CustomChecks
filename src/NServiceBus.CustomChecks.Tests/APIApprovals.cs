using System.Runtime.CompilerServices;
using NServiceBus.CustomChecks;
using NServiceBus.CustomChecks.Tests;
using NUnit.Framework;
using PublicApiGenerator;

[TestFixture]
public class APIApprovals
{
    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Approve()
    {
        var publicApi = ApiGenerator.GeneratePublicApi(typeof(CustomCheck).Assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" });
        TestApprover.Verify(publicApi);
    }
}