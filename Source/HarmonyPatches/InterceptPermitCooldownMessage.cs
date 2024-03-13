using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AutoPermits.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AutoPermits.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.RoyaltyTrackerTick))]
public static class InterceptPermitCooldownMessage
{
    private static void Replacement(string text, LookTargets lookTargets, MessageTypeDef def, bool historical, FactionPermit permit)
    {
        if (!permit.TryTriggerPermit(lookTargets))
            Messages.Message(text, lookTargets, def, historical);
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, MethodBase baseMethod)
    {
        var locals = baseMethod.GetMethodBody()?.LocalVariables;
        if (locals == null)
            throw new Exception($"Trying to patch a method without a body: {baseMethod.GetNameWithNamespace()}");

        var target = AccessTools.DeclaredMethod(typeof(Messages), nameof(Messages.Message), [typeof(string), typeof(LookTargets), typeof(MessageTypeDef), typeof(bool)]);
        var replacement = MethodUtil.MethodOf(Replacement);
        var index = locals.FirstOrDefault(l => l.LocalType == typeof(FactionPermit))?.LocalIndex ?? -1;
        var patchCount = 0;

        if (index < 0)
            throw new Exception($"[{AutoPermitsModCore.ModName}] - Failed to find correct index for the local of type {nameof(FactionPermit)}");

        foreach (var ci in instr)
        {
            if (ci.Calls(target))
            {
                ci.opcode = OpCodes.Call;
                ci.operand = replacement;

                yield return TranspilerUtil.GetLdlocForIndex(index);
                patchCount++;
            }

            yield return ci;
        }

        const int expectedPatches = 1;
        if (patchCount != expectedPatches)
            Log.Error($"[{AutoPermitsModCore.ModName}] - patched incorrect number of calls to Messages.Message (expected: {expectedPatches}, patched: {patchCount}) for method {baseMethod.GetNameWithNamespace()}");
    }
}