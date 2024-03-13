using System.Collections.Generic;
using Verse;

namespace AutoPermits.Utilities;

public static class CollectionUtil
{
    public static IEnumerable<T> ConcatIfNotNull<T>(this IEnumerable<T> lhs, T rhs)
        => rhs == null ? lhs : lhs.Concat(rhs);
}