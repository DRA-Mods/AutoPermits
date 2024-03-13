using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AutoPermits.Dialogs;
using AutoPermits.Utilities;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoPermits.HarmonyPatches;

[HarmonyPatch]
public static class InsertManageAutoPermitOption
{
    private static MethodBase TargetMethod()
    {
        var method = MethodUtil.GetLambda(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.RoyalAidGizmo), lambdaOrdinal: 0);
        if (method.ReturnType != typeof(void))
            Log.Error($"[{AutoPermitsModCore.ModName}] - Configure float menu option may not work, Pawn_RoyaltyTracker.RoyalAidGizmo lambda to return void, actual value: {method.ReturnType}");

        return method;
    }

    private static void InsertOption(List<FloatMenuOption> list, Pawn_RoyaltyTracker royalty)
    {
        if (!royalty.factionPermits.Any(PermitUtil.IsResourcePermit))
            return;

        var option = new FloatMenuOption(
            "AP_ConfigurePermits".Translate(),
            () => Find.WindowStack.Add(new Dialog_ConfigureAutoPermits(royalty.pawn)),
            MenuOptionPriority.VeryLow);
        list.Add(option);
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, MethodBase baseMethod)
    {
        var locals = baseMethod.GetMethodBody()?.LocalVariables;
        if (locals == null)
            throw new Exception($"Trying to patch a method without a body: {baseMethod.GetNameWithNamespace()}");

        var insertion = MethodUtil.MethodOf(InsertOption);
        var index = locals.FirstOrDefault(l => l.LocalType == typeof(List<FloatMenuOption>))?.LocalIndex ?? -1;
        var patchCount = 0;

        if (index < 0)
            throw new Exception($"[{AutoPermitsModCore.ModName}] - Failed to find correct index for the local of type List<string>");

        foreach (var ci in instr)
        {
            yield return ci;

            if (ci.IsStloc(index))
            {
                yield return TranspilerUtil.GetLdlocForIndex(index); // Load in the list
                yield return new CodeInstruction(OpCodes.Ldarg_0); // Load in "this"
                yield return new CodeInstruction(OpCodes.Call, insertion);

                patchCount++;
            }
        }

        const int expectedPatches = 1;
        if (patchCount != expectedPatches)
            Log.Error($"[{AutoPermitsModCore.ModName}] - patched incorrect number of calls to Messages.Message (expected: {expectedPatches}, patched: {patchCount}) for method {baseMethod.GetNameWithNamespace()}");
    }
}