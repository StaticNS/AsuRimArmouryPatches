using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Selector), "SelectUnderMouse")]
internal static class Patch_Selector_SquadCell
{
	private static void Postfix()
	{
		if (SquadBar.CellRole(Find.ColonistBar.ColonistOrCorpseAt(UI.MousePositionOnUIInverted) as Pawn, out var squad) != 0)
		{
			SquadBar.SelectSquad(squad);
		}
	}
}
