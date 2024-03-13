using Prepatcher;
using RimWorld;

namespace AutoPermits.Utilities;

public static class RoyaltyUtil
{
    [PrepatcherField]
    [DefaultValue(false)]
    public static extern ref bool UsePermitsOnCaravans(this Pawn_RoyaltyTracker royalty);

    [PrepatcherField]
    [DefaultValue(true)]
    public static extern ref bool UsePermitsOnColonyMapsOnly(this Pawn_RoyaltyTracker royalty);
}