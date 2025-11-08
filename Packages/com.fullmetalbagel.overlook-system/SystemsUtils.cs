#nullable enable

using System;
using System.Collections.Generic;

namespace Overlook.System;

public static class SystemsUtils
{
    public static IReadOnlyDictionary<Guid, Type> IdTypeMap { get; }

    static SystemsUtils()
    {
        Dictionary<Guid, Type> dict = new();
        foreach (var (guid, type) in TypeGuidUtils.IdTypeMap)
        {
            if (typeof(ISystem).IsAssignableFrom(type))
                dict[guid] = type;
        }
        IdTypeMap = dict;
    }
}
