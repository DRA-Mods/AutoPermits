using System.Collections.Generic;
using AutoPermits.Dialogs;
using AutoPermits.Utilities;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace AutoPermits.HarmonyPatches;

[HarmonyPatch(typeof(Caravan), nameof(Caravan.GetGizmos))]
public static class CaravanGizmos
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Caravan __instance)
        => gizmos.ConcatIfNotNull(GetConfigureGizmo(__instance));

    private static Gizmo GetConfigureGizmo(Caravan caravan)
    {
        if (caravan.PawnsListForReading.Any(pawn => pawn.royalty?.AllFactionPermits.Any(permit => permit.IsResourcePermit()) == true))
        {
            return new Command_Action
            {
                action = () => OpenPawnPickerFloatMenu(caravan),
                defaultLabel = "AP_AutoPermitForPawnTitle".Translate(),
                icon = Resources.CommandTex.Texture,
            };
        }

        return null;
    }

    private static void OpenPawnPickerFloatMenu(Caravan caravan)
    {
        var list = SimplePool<List<FloatMenuOption>>.Get();

        foreach (var pawn in caravan.PawnsListForReading)
        {
            if (pawn.royalty?.AllFactionPermits.Any(p => p.IsResourcePermit()) == true)
            {
                list.Add(new FloatMenuOption(
                    "AP_AutoPermitForPawnOption".Translate(pawn.Named("PAWN")),
                    () => Find.WindowStack.Add(new Dialog_ConfigureAutoPermits(pawn))));
            }
        }

        if (list.Count != 1)
        {
            if (list.Count <= 0)
                list.Add(new FloatMenuOption("AP_AutoPermitForPawnNoOptions".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(list));
        }
        else list[0].action();

        list.Clear();
        SimplePool<List<FloatMenuOption>>.Return(list);
    }
}