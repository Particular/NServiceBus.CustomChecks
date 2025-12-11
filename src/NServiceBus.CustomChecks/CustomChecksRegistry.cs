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
            
            foreach (var checkType in availableTypes.Where(IsCustomCheck))
            {
                allCheckTypes.Add(checkType);
            }
        }

        static bool IsCustomCheck(Type t) => typeof(ICustomCheck).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface);

        public void AddCheck<TCustomCheck>() where TCustomCheck : class, ICustomCheck => allCheckTypes.Add(typeof(TCustomCheck));

        public IEnumerable<Type> GetAllCheckTypes() => allCheckTypes.ToList();

        public IReadOnlyList<ICustomCheckWrapper> ResolveWrappers(IServiceProvider provider)
        {
            var wrappers = new List<ICustomCheckWrapper>(allCheckTypes.Count);

            foreach (var checkType in allCheckTypes)
            {
                var wrapperType = typeof(CustomCheckWrapper<>).MakeGenericType(checkType);
                var wrapper = (ICustomCheckWrapper)Activator.CreateInstance(wrapperType)!;
                wrapper.Initialize(provider);
                wrappers.Add(wrapper);
            }

            return wrappers;
        }
    }
}