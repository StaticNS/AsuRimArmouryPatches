using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawColonist")]
internal static class Patch_ColonistBar_DrawSquadCell
{
	private static bool Prefix(ColonistBarColonistDrawer __instance, Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering)
	{
		int num = SquadBar.CellRole(colonist, out var squad);
		if (num == 0)
		{
			return true;
		}
		WeaponIconSuppressor.SuppressNextFor(rect);
		if (num == 2)
		{
			return false;
		}
		SquadBar.DrawCell(__instance, rect, colonist, squad, pawnMap, highlight, reordering);
		TooltipHandler.TipRegion(SquadBar.PanelRect(rect, colonist, squad), () => SquadBar.Tooltip(squad), 5898240 + squad.id);
		return false;
	}
}
