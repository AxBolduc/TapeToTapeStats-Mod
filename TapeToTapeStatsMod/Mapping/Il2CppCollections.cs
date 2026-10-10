using Il2CppInterop.Runtime.InteropTypes;
using Il2CppReadOnlyCollection = Il2CppSystem.Collections.Generic;

namespace TapeToTapeStatsMod.Mapping;

public static class Il2CppCollections
{
    /// <summary>
    /// Copies an Il2Cpp <c>IReadOnlyList&lt;T&gt;</c> into a managed list. The interop wrapper only exposes the
    /// indexer, so the count comes from its <c>IReadOnlyCollection&lt;T&gt;</c> view.
    /// </summary>
    public static List<T> ToManagedList<T>(Il2CppReadOnlyCollection.IReadOnlyList<T>? list)
        where T : Il2CppObjectBase
    {
        var items = new List<T>();

        if (list == null)
        {
            return items;
        }

        var count = list.Cast<Il2CppReadOnlyCollection.IReadOnlyCollection<T>>().Count;

        for (var index = 0; index < count; index++)
        {
            items.Add(list[index]);
        }

        return items;
    }

    public static List<string> ToManagedList(Il2CppReadOnlyCollection.List<string>? list)
    {
        var items = new List<string>();

        if (list == null)
        {
            return items;
        }

        for (var index = 0; index < list.Count; index++)
        {
            items.Add(list[index]);
        }

        return items;
    }
}
