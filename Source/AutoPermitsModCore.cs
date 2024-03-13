using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace AutoPermits;

public class AutoPermitsModCore : Mod
{
    public const string ModName = "AutoPermits";

    internal static Harmony Harmony { get; } = new("Dra.AutoPermits");

    public AutoPermitsModCore(ModContentPack content) : base(content)
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Harmony.PatchAll();
            if (MP.enabled)
                MP.RegisterAll();
        });
    }
}