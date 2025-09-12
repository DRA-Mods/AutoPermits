using System;
using AutoPermits.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AutoPermits.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.RoyaltyTrackerTickInterval))]
public static class CaravanPermitUse
{
    public static void Postfix(Pawn_RoyaltyTracker __instance, int delta)
    {
        // If a pawn is spawned on a map, the check will be done each tick anyway.
        // Also check if the pawn will ever use the permits on caravan, and (like vanilla) exclude animals from this.
        // TODO: Seems vanilla isn't always triggering the permit messages due to VTR, revert once it's fixed
        // if (!__instance.pawn.IsCaravanMember() || !__instance.UsePermitsOnCaravans() || __instance.pawn.RaceProps.Animal)
        //     return;

        // Same loop that's used in Vanilla, but only when a pawn is spawned and non-animal
        foreach (var permit in __instance.AllFactionPermits)
        {
            if (permit.LastUsedTick > 0 && Math.Abs(Find.TickManager.TicksGame - permit.LastUsedTick - permit.Permit.CooldownTicks) < delta)
                permit.TryTriggerPermit(__instance.pawn);
        }
    }
}