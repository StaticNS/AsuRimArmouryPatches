using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "HandleClicks")]
internal static class Patch_ColonistBar_SquadClicks
{
	private static void Prefix(ref Rect rect, Pawn colonist)
	{
		int num = SquadBar.CellRole(colonist, out var squad);
		if (num != 0)
		{
			rect = SquadBar.ClickRect(rect, colonist, squad, num);
			if (Event.current.type == EventType.mouseDown && Event.current.button == 0 && Event.current.shift && Mouse.IsOver(rect))
			{
				Event.current.Use();
				SquadBar.OpenMemberMenu(squad);
			}
		}
	}
}
