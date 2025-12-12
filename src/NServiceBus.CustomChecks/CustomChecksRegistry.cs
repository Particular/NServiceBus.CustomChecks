namespace NServiceBus.CustomChecks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class CustomChecksRegistry
    {
        readonly HashSet<ICustomCheckWrapper> wrappers = [];

        public void AddScannedTypes(IEnumerable<Type> availableTypes)
        {
            ArgumentNullException.ThrowIfNull(availableTypes);

            foreach (var checkType in availableTypes.Where(IsCustomCheck))
            {
                var wrapperType = typeof(CustomCheckWrapper<>).MakeGenericType(checkType);
                var wrapper = (ICustomCheckWrapper)Activator.CreateInstance(wrapperType)!;
                wrappers.Add(wrapper);
            }
        }

        static bool IsCustomCheck(Type t) => typeof(ICustomCheck).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface);

        public void AddCheck<TCustomCheck>() where TCustomCheck : class, ICustomCheck
        {
            var wrapper = new CustomCheckWrapper<TCustomCheck>();
            wrappers.Add(wrapper);
        }

        public IEnumerable<Type> GetAllCheckTypes() => wrappers.Select(w => w.CheckType).ToList();

        public IReadOnlyList<ICustomCheckWrapper> ResolveWrappers(IServiceProvider provider)
        {
            foreach (var wrapper in wrappers)
            {
                wrapper.Initialize(provider);
            }

            return wrappers.ToList();
        }
    }
}