using System.Reflection;
using Verse;

namespace AutoPermits.Utilities;

public static class ReflectionUtil
{
    public static string GetNameWithNamespace(this MethodBase method)
        => (method.DeclaringType?.Namespace).NullOrEmpty() ? method.Name : $"{method.DeclaringType.Name}:{method.Name}";
}