namespace NServiceBus.CustomChecks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class CustomChecksRegistry
    {
        readonly HashSet<Type> allCheckTypes = [];

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
        /// <typeparam name="TCustomCheck">The custom check type to add.</typeparam>
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
    }
}

