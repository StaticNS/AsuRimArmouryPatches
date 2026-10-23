using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(InspectPaneUtility), "AdjustedLabelFor")]
internal static class Patch_InspectPane_SquadLabel
{
	private static void Postfix(List<object> selected, ref string __result)
	{
		if (selected != null && selected.Count >= 2)
		{
			Squad squad = MatchingSquad(selected);
			if (squad != null)
			{
				__result = squad.label + " x" + selected.Count;
			}
		}
	}

	private static Squad MatchingSquad(List<object> selected)
	{
		SquadManager current = SquadManager.Current;
		if (current == null || !(selected[0] is Pawn pawn))
		{
			return null;
		}
		Squad squad = current.SquadOf(pawn);
		if (squad == null)
		{
			return null;
		}
		List<Pawn> members = squad.Members;
		if (members.Count != selected.Count)
		{
			return null;
		}
		for (int i = 0; i < selected.Count; i++)
		{
			if (!(selected[i] is Pawn item) || !members.Contains(item))
			{
				return null;
			}
		}
		return squad;
	}
}
