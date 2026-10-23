using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(ColonistBar), "TryGetEntryAt")]
internal static class Patch_ColonistBar_SquadHitTest
{
	private static void Postfix(ColonistBar __instance, Vector2 pos, ref ColonistBar.Entry entry, ref bool __result)
	{
		if (__result)
		{
			return;
		}
		SquadManager current = SquadManager.Current;
		if (current == null)
		{
			return;
		}
		List<Squad> squads = current.Squads;
		for (int i = 0; i < squads.Count; i++)
		{
			if (SquadBar.TryPanelRect(squads[i], out var panel, out var anchor) && panel.Contains(pos))
			{
				int num = SquadBar.IndexOf(__instance.cachedEntries, anchor);
				if (num >= 0)
				{
					entry = __instance.cachedEntries[num];
					__result = true;
					break;
				}
			}
		}
	}
}
