namespace NServiceBus.CustomChecks.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.CustomChecks;
    using NUnit.Framework;

    /// <summary>
    /// Unit tests for CustomCheckRegistry.
    /// </summary>
    [TestFixture]
    public class CustomCheckRegistryTests
    {
        /// <summary>
        /// Verifies that AddScannedTypes throws ArgumentNullException when given null.
        /// </summary>
        [Test]
        public void AddScannedTypes_should_throw_when_availableTypes_is_null()
        {
            var registry = new CustomCheckRegistry();

            Assert.Throws<ArgumentNullException>(() => registry.AddScannedTypes(null));
        }

        /// <summary>
        /// Verifies that AddScannedTypes filters out abstract classes.
        /// </summary>
        [Test]
        public void AddScannedTypes_should_filter_out_abstract_classes()
        {
            var registry = new CustomCheckRegistry();
            var types = new[] { typeof(AbstractCustomCheck) };

            registry.AddScannedTypes(types);

            Assert.That(registry.Count, Is.EqualTo(0));
            Assert.That(registry.GetAllCheckTypes(), Is.Empty);
        }

        /// <summary>
        /// Verifies that AddScannedTypes filters out interfaces.
        /// </summary>
        [Test]
        public void AddScannedTypes_should_filter_out_interfaces()
        {
            var registry = new CustomCheckRegistry();
            var types = new[] { typeof(ICustomCheck) };

            registry.AddScannedTypes(types);

            Assert.That(registry.Count, Is.EqualTo(0));
            Assert.That(registry.GetAllCheckTypes(), Is.Empty);
        }

        /// <summary>
        /// Verifies that AddScannedTypes only adds concrete ICustomCheck implementations.
        /// </summary>
        [Test]
        public void AddScannedTypes_should_only_add_concrete_ICustomCheck_implementations()
        {
            var registry = new CustomCheckRegistry();
            var types = new[]
            {
                typeof(ConcreteCustomCheck),
                typeof(AbstractCustomCheck),
                typeof(ICustomCheck),
                typeof(string), // Not an ICustomCheck
                typeof(int) // Not an ICustomCheck
            };

            registry.AddScannedTypes(types);

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.GetAllCheckTypes(), Contains.Item(typeof(ConcreteCustomCheck)));
        }

        /// <summary>
        /// Verifies that AddScannedTypes handles empty collection.
        /// </summary>
        [Test]
        public void AddScannedTypes_should_handle_empty_collection()
        {
            var registry = new CustomCheckRegistry();
            var types = Array.Empty<Type>();

            registry.AddScannedTypes(types);

            Assert.That(registry.Count, Is.EqualTo(0));
            Assert.That(registry.GetAllCheckTypes(), Is.Empty);
        }

        /// <summary>
        /// Verifies that AddCheck adds the type to the registry.
        /// </summary>
        [Test]
        public void AddCheck_should_add_type_to_registry()
        {
            var registry = new CustomCheckRegistry();

            registry.AddCheck<ConcreteCustomCheck>();

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.GetAllCheckTypes(), Contains.Item(typeof(ConcreteCustomCheck)));
        }

        /// <summary>
        /// Verifies that AddCheck handles deduplication when same type is added twice.
        /// </summary>
        [Test]
        public void AddCheck_should_deduplicate_when_same_type_added_twice()
        {
            var registry = new CustomCheckRegistry();

            registry.AddCheck<ConcreteCustomCheck>();
            registry.AddCheck<ConcreteCustomCheck>();

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.GetAllCheckTypes().Count(), Is.EqualTo(1));
            Assert.That(registry.GetAllCheckTypes(), Contains.Item(typeof(ConcreteCustomCheck)));
        }

        /// <summary>
        /// Verifies that GetAllCheckTypes returns a copy, not the internal collection.
        /// </summary>
        [Test]
        public void GetAllCheckTypes_should_return_copy_not_internal_collection()
        {
            var registry = new CustomCheckRegistry();
            registry.AddCheck<ConcreteCustomCheck>();

            var firstCall = registry.GetAllCheckTypes().ToList();
            var secondCall = registry.GetAllCheckTypes().ToList();

            // Should be different instances
            Assert.That(firstCall, Is.Not.SameAs(secondCall));
            // But should contain the same items
            Assert.That(firstCall, Is.EqualTo(secondCall));
        }

        /// <summary>
        /// Verifies that Count property returns accurate count.
        /// </summary>
        [Test]
        public void Count_should_return_accurate_count()
        {
            var registry = new CustomCheckRegistry();

            Assert.That(registry.Count, Is.EqualTo(0));

            registry.AddCheck<ConcreteCustomCheck>();
            Assert.That(registry.Count, Is.EqualTo(1));

            registry.AddCheck<AnotherConcreteCustomCheck>();
            Assert.That(registry.Count, Is.EqualTo(2));

            // Adding duplicate should not increase count
            registry.AddCheck<ConcreteCustomCheck>();
            Assert.That(registry.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Verifies that registry handles both scanned and manually registered types with deduplication.
        /// </summary>
        [Test]
        public void Should_deduplicate_when_same_type_added_via_both_scanning_and_manual_registration()
        {
            var registry = new CustomCheckRegistry();

            // Add manually first
            registry.AddCheck<ConcreteCustomCheck>();

            // Then add via scanning (same type)
            registry.AddScannedTypes(new[] { typeof(ConcreteCustomCheck) });

            // Should only have one instance
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.GetAllCheckTypes().Count(), Is.EqualTo(1));
        }

        /// <summary>
        /// Verifies that registry handles multiple types from scanning.
        /// </summary>
        [Test]
        public void AddScannedTypes_should_add_multiple_valid_types()
        {
            var registry = new CustomCheckRegistry();
            var types = new[]
            {
                typeof(ConcreteCustomCheck),
                typeof(AnotherConcreteCustomCheck),
                typeof(AbstractCustomCheck), // Should be filtered
                typeof(string) // Should be filtered
            };

            registry.AddScannedTypes(types);

            Assert.That(registry.Count, Is.EqualTo(2));
            Assert.That(registry.GetAllCheckTypes(), Contains.Item(typeof(ConcreteCustomCheck)));
            Assert.That(registry.GetAllCheckTypes(), Contains.Item(typeof(AnotherConcreteCustomCheck)));
        }
    }

    /// <summary>
    /// Abstract custom check for testing filtering.
    /// </summary>
    abstract class AbstractCustomCheck : ICustomCheck
    {
        public string Category => "Test";
        public string Id => "Abstract";
        public TimeSpan? Interval => null;
        public Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    /// <summary>
    /// Concrete custom check for testing.
    /// </summary>
    class ConcreteCustomCheck : CustomCheck
    {
        public ConcreteCustomCheck() : base("Concrete", "Test")
        {
        }

        public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckResult.Pass);
        }
    }

    /// <summary>
    /// Another concrete custom check for testing.
    /// </summary>
    class AnotherConcreteCustomCheck : CustomCheck
    {
        public AnotherConcreteCustomCheck() : base("Another", "Test")
        {
        }

        public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckResult.Pass);
        }
    }
}

