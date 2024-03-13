using AutoPermits.Utilities;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AutoPermits.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.RoyaltyTrackerTick))]
public static class CaravanPermitUse
{
    public static void Postfix(Pawn_RoyaltyTracker __instance)
    {
        // If a pawn is spawned on a map, the check will be done each tick anyway.
        // Also check if the pawn will ever use the permits on caravan, and (like vanilla) exclude animals from this.
        if (!__instance.pawn.IsCaravanMember() || !__instance.UsePermitsOnCaravans() || __instance.pawn.RaceProps.Animal)
            return;

        // Same loop that's used in Vanilla, but only when a pawn is spawned and non-animal
        foreach (var permit in __instance.AllFactionPermits)
        {
            if (permit.LastUsedTick > 0 && Find.TickManager.TicksGame == permit.LastUsedTick + permit.Permit.CooldownTicks)
                permit.TryTriggerPermit(__instance.pawn);
        }
    }
}