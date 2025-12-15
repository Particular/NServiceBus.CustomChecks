namespace NServiceBus.CustomChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

sealed class CustomChecksRegistry
{
    readonly HashSet<ICustomCheckWrapper> customChecks = [];

    public void AddScannedTypes(IEnumerable<Type> availableTypes)
    {
        ArgumentNullException.ThrowIfNull(availableTypes);

        foreach (var checkType in availableTypes.Where(IsCustomCheck))
        {
            _ = AddCustomCheckMethodInfo.MakeGenericMethod(checkType).Invoke(this, []);
        }
    }

    static bool IsCustomCheck(Type t) => typeof(ICustomCheck).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface);

    public void AddCheck<TCustomCheck>() where TCustomCheck : class, ICustomCheck => customChecks.Add(new CustomCheckWrapper<TCustomCheck>());

    public IReadOnlyCollection<Type> GetAllCheckTypes() => [.. customChecks.Select(w => w.CheckType)];

    public IReadOnlyCollection<ICustomCheckWrapper> Initialize(IServiceProvider provider)
    {
        foreach (var check in customChecks)
        {
            check.Initialize(provider);
        }

        return [.. customChecks];
    }

    static readonly MethodInfo AddCustomCheckMethodInfo = typeof(CustomChecksRegistry).GetMethod(nameof(AddCheck))!;
}