namespace NServiceBus.CustomChecks.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using NServiceBus;
    using NServiceBus.CustomChecks;
    using NUnit.Framework;

    /// <summary>
    /// Tests for the manual registration API of custom checks.
    /// </summary>
    [TestFixture]
    public class ManualRegistrationTests
    {
        /// <summary>
        /// Verifies that a single custom check can be manually registered.
        /// </summary>
        [Test]
        public void Should_add_custom_check_manually()
        {
            var config = new EndpointConfiguration("TestEndpoint");

            // Add custom check manually
            config.AddCustomCheck<TestCustomCheck>();

            // Verify the registry was created and stored in settings
            var registry = config.GetSettings().Get<CustomCheckRegistry>(typeof(CustomCheckRegistry).FullName);
            var registeredTypes = registry.GetAllCheckTypes();

            // Assert the check was properly registered
            Assert.That(registeredTypes, Contains.Item(typeof(TestCustomCheck)));
        }

        /// <summary>
        /// Verifies that multiple custom checks can be manually registered.
        /// </summary>
        [Test]
        public void Should_add_multiple_custom_checks()
        {
            var config = new EndpointConfiguration("TestEndpoint");

            // Add multiple custom checks
            config.AddCustomCheck<TestCustomCheck>();
            config.AddCustomCheck<AnotherTestCustomCheck>();

            // Verify all checks were registered in the same registry
            var registry = config.GetSettings().Get<CustomCheckRegistry>(typeof(CustomCheckRegistry).FullName);
            var registeredTypes = registry.GetAllCheckTypes();

            // Assert both checks were properly registered
            Assert.That(registeredTypes, Contains.Item(typeof(TestCustomCheck)));
            Assert.That(registeredTypes, Contains.Item(typeof(AnotherTestCustomCheck)));
            Assert.That(registeredTypes.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Verifies that an ArgumentNullException is thrown when the EndpointConfiguration is null.
        /// </summary>
        [Test]
        public void Should_throw_when_config_is_null()
        {
            EndpointConfiguration config = null;

            // Attempt to call AddCustomCheck with a null config
            Assert.Throws<ArgumentNullException>(() => config.AddCustomCheck<TestCustomCheck>());
        }

        /// <summary>
        /// Verifies that the registry can handle both manually registered and assembly scanned types.
        /// </summary>
        [Test]
        public void Should_handle_both_manual_and_scanned_types()
        {
            var registry = new CustomCheckRegistry();

            // Add manually registered types
            registry.AddCheck<TestCustomCheck>();
            registry.AddCheck<AnotherTestCustomCheck>();

            // Add assembly scanned types
            var scannedTypes = new[] { typeof(TestCustomCheck), typeof(AnotherTestCustomCheck), typeof(ScannedTestCustomCheck) };
            registry.AddScannedTypes(scannedTypes);

            // Get all types from the registry
            var allTypes = registry.GetAllCheckTypes().ToList();

            // Should contain all types, with no duplicates
            Assert.That(allTypes, Contains.Item(typeof(TestCustomCheck)));
            Assert.That(allTypes, Contains.Item(typeof(AnotherTestCustomCheck)));
            Assert.That(allTypes, Contains.Item(typeof(ScannedTestCustomCheck)));
            Assert.That(allTypes.Count, Is.EqualTo(3));
        }
    }

    /// <summary>
    /// Test implementation of ICustomCheck for unit testing.
    /// </summary>
    class TestCustomCheck : CustomCheck
    {
        public TestCustomCheck() : base("TestCheck", "TestCategory")
        {
        }

        public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckResult.Pass);
        }
    }

    /// <summary>
    /// Another test implementation of ICustomCheck for unit testing.
    /// </summary>
    class AnotherTestCustomCheck : CustomCheck
    {
        public AnotherTestCustomCheck() : base("AnotherTestCheck", "TestCategory")
        {
        }

        public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckResult.Pass);
        }
    }

    /// <summary>
    /// Test implementation of ICustomCheck for unit testing assembly scanning.
    /// </summary>
    class ScannedTestCustomCheck : CustomCheck
    {
        public ScannedTestCustomCheck() : base("ScannedTestCheck", "ScannedCategory")
        {
        }

        public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckResult.Pass);
        }
    }
}
