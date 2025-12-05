namespace NServiceBus.CustomChecks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Central registry for all custom checks (assembly scanned and manually registered).
    /// Single source of truth for custom check discovery.
    /// </summary>
    class CustomChecksRegistry
    {
        // All custom check types (scanned and manually registered)
        readonly HashSet<Type> allCheckTypes = [];

        /// <summary>
        /// Adds assembly scanned custom check types to the registry.
        /// Filters out abstract classes and interfaces.
        /// </summary>
        /// <param name="availableTypes">All available types from the assembly scan.</param>
        public void AddScannedTypes(IEnumerable<Type> availableTypes)
        {
            ArgumentNullException.ThrowIfNull(availableTypes);

            // Filter to concrete ICustomCheck implementations
            var customCheckTypes = availableTypes
                .Where(t => typeof(ICustomCheck).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface));

            foreach (var checkType in customCheckTypes)
            {
                allCheckTypes.Add(checkType);
            }
        }

        /// <summary>
        /// Adds a custom check type via manual registration.
        /// </summary>
        /// <typeparam name="TCustomCheck">The custom check type to add. Must implement ICustomCheck.</typeparam>
        public void AddCheck<TCustomCheck>() where TCustomCheck : class, ICustomCheck
        {
            allCheckTypes.Add(typeof(TCustomCheck));
        }

        /// <summary>
        /// Gets all custom check types from the registry.
        /// Automatically handles deduplication.
        /// </summary>
        /// <returns>A list of all custom check types that have been discovered.</returns>
        public IEnumerable<Type> GetAllCheckTypes() => allCheckTypes.ToList();

        /// <summary>
        /// Gets the count of registered custom check types.
        /// </summary>
        /// <returns>The number of custom check types in the registry.</returns>
        public int Count => allCheckTypes.Count;
    }
}

