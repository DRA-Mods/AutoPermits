using System;
using System.Linq;
using HarmonyLib;
using Prepatcher;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AutoPermits.Utilities;

[StaticConstructorOnStartup]
public static class PermitUtil
{
    private static readonly Type StoneDropType;
    private static readonly AccessTools.FieldRef<RoyalTitlePermitWorker_Targeted, Faction> StoneDropFactionField;
    private static readonly ThingDef RoyalPermitDropSpot = DefDatabase<ThingDef>.GetNamedSilentFail("");

    static PermitUtil()
    {
        StoneDropType = AccessTools.TypeByName("VFEEmpire.RoyalTitlePermitWorker_Stone");
        if (StoneDropType == null)
            return;

        if (!typeof(RoyalTitlePermitWorker_Targeted).IsAssignableFrom(StoneDropType))
        {
            StoneDropType = null;
            Log.Warning($"[{AutoPermitsModCore.ModName}] - VFEEmpire.RoyalTitlePermitWorker_Stone is not a subtype of {nameof(RoyalTitlePermitWorker_Targeted)}");
        }
        else
        {
            var field = AccessTools.Field(StoneDropType, "faction");
            if (field != null && !field.IsStatic && typeof(Faction).IsAssignableFrom(field.FieldType))
            {
                try
                {
                    StoneDropFactionField = AccessTools.FieldRefAccess<RoyalTitlePermitWorker_Targeted, Faction>(field);
                }
                catch (Exception e)
                {
                    StoneDropType = null;
                    Log.Warning($"[{AutoPermitsModCore.ModName}] - VFEEmpire.RoyalTitlePermitWorker_Stone caused an exception when accessing faction field:\n{e}");
                }
            }
            else
            {
                StoneDropType = null;
                Log.Warning($"[{AutoPermitsModCore.ModName}] - VFEEmpire.RoyalTitlePermitWorker_Stone is not a faction field, or it's static/of incorrect type.");
            }
        }
    }

    [PrepatcherField]
    [Prepatcher.DefaultValue(false)]
    public static extern ref bool AutoUse(this FactionPermit permit);

    public static bool IsResourcePermit(this FactionPermit permit)
    {
        if (permit.Permit.Worker is RoyalTitlePermitWorker_DropResources)
            return true;
        if (StoneDropType == null)
            return false;
        return StoneDropType.IsInstanceOfType(permit.Permit.Worker);
    }

    public static void TryTriggerAllPermits(LookTargets singleTarget)
    {
        if (singleTarget.targets.Count == 1 && singleTarget.targets[0].Thing is Pawn pawn)
            TryTriggerAllPermits(pawn);
    }

    public static void TryTriggerAllPermits(Pawn pawn)
    {
        // The usual sanity checks
        if (pawn?.royalty?.AllFactionPermits == null)
            return;

        foreach (var permit in pawn.royalty.AllFactionPermits)
            permit.TryTriggerPermit(pawn);
    }

    public static bool TryTriggerPermit(this FactionPermit permit, LookTargets singleTarget)
        => singleTarget.targets.Count == 1 && singleTarget.targets[0].Thing is Pawn pawn && permit.TryTriggerPermit(pawn);

    public static bool TryTriggerPermit(this FactionPermit permit, Pawn pawn)
    {
        if (permit.OnCooldown || !permit.AutoUse() || pawn.royalty == null || pawn.Downed || pawn.IsSlave || pawn.IsQuestLodger())
            return false;

        var caravan = pawn.GetCaravan();
        if (caravan != null)
        {
            if (!pawn.royalty.UsePermitsOnCaravans())
                return false;
            if (permit.Permit.Worker is not RoyalTitlePermitWorker_DropResources caravanPermit)
                return false;

            var mass = caravan.MassUsage + permit.Permit.royalAid.itemsToDrop.Sum(itemToDrop => itemToDrop.thingDef.BaseMass * itemToDrop.count);
            if (mass > caravan.MassCapacity)
                return false;

            caravanPermit.CallResourcesToCaravan(pawn, permit.Faction, true);
            return true;
        }

        if (pawn.Map == null || !pawn.Spawned)
            return false;
        if (pawn.royalty.UsePermitsOnColonyMapsOnly() && pawn.Map.ParentFaction != Faction.OfPlayerSilentFail)
            return false;

        if (permit.Permit.Worker is not RoyalTitlePermitWorker_Targeted targetedPermit)
            return false;

        if (targetedPermit is RoyalTitlePermitWorker_DropResources resourcePermit)
            resourcePermit.faction = permit.Faction;
        else if (StoneDropType != null && StoneDropType.IsInstanceOfType(permit))
            StoneDropFactionField(targetedPermit) = permit.Faction;
        else
            return false;

        targetedPermit.caller = pawn;
        targetedPermit.map = pawn.Map;
        targetedPermit.free = true;

        targetedPermit.OrderForceTarget(GetDropSpot(pawn.Map) ?? DropCellFinder.TradeDropSpot(pawn.Map));
        return true;
    }

    public static IntVec3? GetDropSpot(Map map)
    {
        if (RoyalPermitDropSpot == null)
            return null;
        return map.listerThings.ThingsOfDef(RoyalPermitDropSpot).FirstOrDefault()?.Position;
    }
}