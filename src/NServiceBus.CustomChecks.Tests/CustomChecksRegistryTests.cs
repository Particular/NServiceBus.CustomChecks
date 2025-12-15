namespace NServiceBus.CustomChecks.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.CustomChecks;
    using NUnit.Framework;

    [TestFixture]
    public class CustomChecksRegistryTests
    {
        [Test]
        public void Should_return_scanned_types()
        {
            var registry = new CustomChecksRegistry();
            registry.AddScannedTypes([typeof(CheckA)]);

            var result = registry.GetAllCheckTypes();

            Assert.That(result, Has.Count.EqualTo(1).And.Contain(typeof(CheckA)));
        }

        [Test]
        public void Should_return_manually_registered_types()
        {
            var registry = new CustomChecksRegistry();
            registry.AddCheck<CheckA>();

            var result = registry.GetAllCheckTypes();

            Assert.That(result, Has.Count.EqualTo(1).And.Contain(typeof(CheckA)));

        }

        [Test]
        public void Should_deduplicate_scanned_and_manual_types()
        {
            var registry = new CustomChecksRegistry();
            registry.AddCheck<CheckA>();
            registry.AddScannedTypes([typeof(CheckA)]);

            var result = registry.GetAllCheckTypes();

            Assert.That(result, Has.Count.EqualTo(1).And.Contain(typeof(CheckA)));

        }

        [Test]
        public void Should_filter_invalid_scanned_types()
        {
            var registry = new CustomChecksRegistry();
            registry.AddScannedTypes([typeof(string), typeof(AbstractCheck)]);

            var result = registry.GetAllCheckTypes();

            Assert.That(result, Is.Empty);
        }

        class CheckA() : CustomCheck("CheckA", "CategoryA")
        {
            public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default) => CheckResult.Pass;
        }

        abstract class AbstractCheck : CustomCheck
        {
            protected AbstractCheck() : base("Abstract", "Category") { }
        }
    }
}

