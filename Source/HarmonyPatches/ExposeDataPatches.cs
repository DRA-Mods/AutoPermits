using AutoPermits.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AutoPermits.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.ExposeData))]
public static class ExposeRoyaltyTrackerData
{
    private static void Postfix(Pawn_RoyaltyTracker __instance)
    {
        Scribe_Values.Look(ref __instance.UsePermitsOnCaravans(), "autoPermitsUseOnCaravans", false);
        Scribe_Values.Look(ref __instance.UsePermitsOnColonyMapsOnly(), "autoPermitsUseOnColonyMapsOnly", true);
    }
}

[HarmonyPatch(typeof(FactionPermit), nameof(FactionPermit.ExposeData))]
public static class ExposePermitData
{
    private static void Postfix(FactionPermit __instance)
    {
        Scribe_Values.Look(ref __instance.AutoUse(), "autoPermitsAutoUse", false);
    }
}