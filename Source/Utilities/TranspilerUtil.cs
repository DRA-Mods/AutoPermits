using System;
using System.Reflection.Emit;
using HarmonyLib;

namespace AutoPermits.Utilities;

public static class TranspilerUtil
{
    public static CodeInstruction GetLdlocForIndex(int index)
        => index switch
        {
            < 0 => throw new ArgumentOutOfRangeException(nameof(index), "Argument must be >= 0"),
            0 => new CodeInstruction(OpCodes.Ldloc_0),
            1 => new CodeInstruction(OpCodes.Ldloc_1),
            2 => new CodeInstruction(OpCodes.Ldloc_2),
            3 => new CodeInstruction(OpCodes.Ldloc_3),
            <= byte.MaxValue => new CodeInstruction(OpCodes.Ldloc_S, index),
            <= ushort.MaxValue => new CodeInstruction(OpCodes.Ldloc, index),
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Argument must be <= ushort.MaxValue"),
        };

    public static bool IsStloc(this CodeInstruction ci, int index)
    {
        if (ci.opcode == OpCodes.Stloc_0) return index == 0;
        if (ci.opcode == OpCodes.Stloc_1) return index == 1;
        if (ci.opcode == OpCodes.Stloc_2) return index == 2;
        if (ci.opcode == OpCodes.Stloc_3) return index == 3;
        return (ci.opcode == OpCodes.Stloc_S || ci.opcode == OpCodes.Stloc) && ci.operand is LocalBuilder builder && builder.LocalIndex == index;
    }
}