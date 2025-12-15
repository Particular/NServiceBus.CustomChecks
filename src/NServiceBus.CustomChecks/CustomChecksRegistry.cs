namespace NServiceBus.CustomChecks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    class CustomChecksRegistry
    {
        readonly HashSet<ICustomCheckWrapper> wrappers = [];

        public void AddScannedTypes(IEnumerable<Type> availableTypes)
        {
            ArgumentNullException.ThrowIfNull(availableTypes);

            foreach (var checkType in availableTypes.Where(IsCustomCheck))
            {
                _ = AddCustomCheckMethodInfo.MakeGenericMethod(checkType).Invoke(this, []);
            }
        }

        static bool IsCustomCheck(Type t) => typeof(ICustomCheck).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface);

        public void AddCheck<TCustomCheck>() where TCustomCheck : class, ICustomCheck => wrappers.Add(new CustomCheckWrapper<TCustomCheck>());

        public IReadOnlyCollection<Type> GetAllCheckTypes() => [.. wrappers.Select(w => w.CheckType)];

        public IReadOnlyCollection<ICustomCheckWrapper> Initialize(IServiceProvider provider)
        {
            foreach (var wrapper in wrappers)
            {
                wrapper.Initialize(provider);
            }

            return [.. wrappers];
        }

        static readonly MethodInfo AddCustomCheckMethodInfo = typeof(CustomChecksRegistry).GetMethod(nameof(AddCheck))!;
    }
}