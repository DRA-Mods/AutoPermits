using System.Linq;
using AutoPermits.Utilities;
using Multiplayer.API;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoPermits.Dialogs;

public class Dialog_ConfigureAutoPermits : Window
{
    private const float SingleRowOffset = 20f;
    private const float SingleRowWithSpacingOffset = SingleRowOffset + 4f;
    private const float LineOffset = 2f;

    private Vector2 scrollPos = Vector2.zero;
    private readonly Pawn royalPawn;

    public Dialog_ConfigureAutoPermits(Pawn pawn)
    {
        royalPawn = pawn;
        doCloseX = true;
        optionalTitle = "AP_AutoPermitTitle".Translate();
    }

    public override Vector2 InitialSize => new(600, 800);

    public override void DoWindowContents(Rect inRect)
    {
        // Do not cache the active permits for MP compatibility, as the currently
        // available permits could change due to other player interactions.
        var permits = royalPawn?.royalty?.AllFactionPermits;
        if (permits == null)
        {
            Close();
            return;
        }

        permits = permits.Where(PermitUtil.IsResourcePermit).ToList();
        if (permits.Count == 0)
        {
            Widgets.Label(GetRect(0, inRect), "AP_NoValidPermits".Translate(royalPawn.Named("PAWN")));
            return;
        }

        inRect.SplitHorizontally(SingleRowWithSpacingOffset * 4f + 6f, out var header, out inRect);
        inRect.SplitHorizontally(inRect.height - SingleRowWithSpacingOffset * 1.25f, out inRect, out var footer);

        // Is there any mod that adds permits to factions others than Empire?
        // May as well be safe and add special handling for those just in case.
        var factionCount = permits.Select(p => p.Faction).Distinct().Count();
        permits = (factionCount > 1
            ? permits.OrderBy(p => p.Faction.def.defName).ThenBy(p => p.Permit.defName)
            : permits.OrderBy(p => p.Permit.defName)).ToList();

        var height = permits.Count * SingleRowWithSpacingOffset;
        if (factionCount > 1) height += factionCount * SingleRowWithSpacingOffset + (factionCount - 1) * LineOffset * 2;

        Faction currentFaction = null;
        var pos = 0f;

        DrawHeader(header);

        Widgets.BeginScrollView(inRect, ref scrollPos, new Rect(0, 0, inRect.width, height));

        foreach (var permit in permits)
        {
            // If there's permits from 2 or more factions, there'll be categories
            if (factionCount > 1)
                DrawFaction(inRect, ref pos, permit, ref currentFaction);
            DrawPermit(inRect, ref pos, permit);
        }

        Widgets.EndScrollView();

        DrawFooter(footer);
    }

    private void DrawHeader(Rect inRect)
    {
        Widgets.BeginGroup(inRect);

        Widgets.Label(GetRect(0, inRect), "AP_AutoPermitExplanation".Translate());

        var prev = royalPawn.royalty.UsePermitsOnCaravans();
        var value = prev;
        var rect = GetRect(SingleRowWithSpacingOffset, inRect);
        Widgets.CheckboxLabeled(rect, "AP_UseOnCaravan".Translate(), ref value);
        TooltipHandler.TipRegion(rect, "AP_UseOnCaravan_Tooltip".Translate());
        if (value != prev)
            SetUseOnCaravan(royalPawn, value);

        prev = royalPawn.royalty.UsePermitsOnColonyMapsOnly();
        value = prev;
        rect = GetRect(SingleRowWithSpacingOffset * 2, inRect);
        Widgets.CheckboxLabeled(rect, "AP_ColonyMapsOnly".Translate(), ref value);
        TooltipHandler.TipRegion(rect, "AP_ColonyMapsOnly_Tooltip".Translate());
        if (value != prev)
            SetColonyMapsOnly(royalPawn, value);

        Widgets.DrawLineHorizontal(0, SingleRowWithSpacingOffset * 3.5f, inRect.width);

        Widgets.EndGroup();
    }

    private void DrawFooter(Rect inRect)
    {
        Widgets.BeginGroup(inRect);

        var text = "AP_TriggerAllPermits".Translate();
        var size = text.GetWidthCached() + 6f;
        var halfSize = size / 2f;
        // Display the button at the center of the dialog
        if (Widgets.ButtonText(new Rect(inRect.center.x - halfSize, 0, size, inRect.height), text))
            TriggerAllPermits(royalPawn);

        Widgets.EndGroup();
    }

    private void DrawFaction(Rect inRect, ref float pos, FactionPermit permit, ref Faction currentFaction)
    {
        // If a faction is changed, or a new one is picked
        if (currentFaction == permit.Faction || pos <= 0)
            return;

        if (pos > 0)
        {
            pos += LineOffset;
            if (scrollPos.y - LineOffset <= pos && scrollPos.y + inRect.height >= pos)
                Widgets.DrawLineHorizontal(0, pos, inRect.width);
            pos += LineOffset;
        }

        currentFaction = permit.Faction;

        if (ShouldDraw(pos, inRect))
            Widgets.Label(GetRect(pos, inRect), currentFaction.NameColored);

        pos += SingleRowOffset;
    }

    private void DrawPermit(Rect inRect, ref float pos, FactionPermit permit)
    {
        if (ShouldDraw(pos, inRect))
        {
            var prev = permit.AutoUse();
            var value = prev;

            Widgets.CheckboxLabeled(GetRect(pos, inRect), permit.Permit.LabelCap, ref prev);
            if (prev != value)
                SetAutoPermitState(royalPawn, permit.Permit, permit.Faction, prev);
        }

        pos += SingleRowOffset;
    }

    [SyncMethod]
    private static void SetAutoPermitState(Pawn pawn, RoyalTitlePermitDef permitDef, Faction faction, bool autoUsePermit)
    {
        // The usual sanity checks
        if (pawn?.royalty?.AllFactionPermits == null || permitDef == null || faction == null)
            return;

        var permit = pawn.royalty.GetPermit(permitDef, faction);
        // Check if the pawn still has the permit, especially useful for MP as the call may end up delayed
        if (permit != null)
            permit.AutoUse() = autoUsePermit;
    }

    [SyncMethod]
    private static void SetUseOnCaravan(Pawn pawn, bool value)
    {
        // The usual sanity checks
        if (pawn?.royalty != null)
            pawn.royalty.UsePermitsOnCaravans() = value;
    }

    [SyncMethod]
    private static void SetColonyMapsOnly(Pawn pawn, bool value)
    {
        // The usual sanity checks
        if (pawn?.royalty != null)
            pawn.royalty.UsePermitsOnColonyMapsOnly() = value;
    }

    [SyncMethod]
    private static void TriggerAllPermits(Pawn pawn) => PermitUtil.TryTriggerAllPermits(pawn);

    private static Rect GetRect(float pos, Rect inRect) => new(0, pos, inRect.width, SingleRowOffset);

    private bool ShouldDraw(float pos, Rect inRect) => scrollPos.y - SingleRowOffset <= pos && scrollPos.y + inRect.height >= pos;
}