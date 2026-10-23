using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
internal static class Patch_ColonistBar_HideCollapsed
{
	private static int processedVersion = -1;

	private static ColonistBar processedBar;

	private static void Prefix(ColonistBar __instance, out bool __state)
	{
		__state = __instance.entriesDirty;
	}

	private static void Postfix(ColonistBar __instance, bool __state)
	{
		if (!__state && processedVersion == Squad.Version && processedBar == __instance)
		{
			return;
		}
		processedVersion = Squad.Version;
		processedBar = __instance;
		SquadManager current = SquadManager.Current;
		if (current == null || current.Squads.Count == 0)
		{
			SquadBar.RecacheAnchors(__instance.cachedEntries, null);
			return;
		}
		try
		{
			Process(__instance, current);
		}
		finally
		{
			SquadBar.RecacheAnchors(__instance.cachedEntries, current.Squads);
		}
	}

	private static void Process(ColonistBar __instance, SquadManager manager)
	{
		List<ColonistBar.Entry> cachedEntries = __instance.cachedEntries;
		bool flag = false;
		List<Squad> squads = manager.Squads;
		for (int i = 0; i < squads.Count; i++)
		{
			Squad squad = squads[i];
			if (!squad.collapsed)
			{
				continue;
			}
			SquadBar.Anchors(squad, cachedEntries, out var first, out var second);
			if (first == null)
			{
				Pawn pawn = FirstDisplayable(squad);
				if (pawn == null)
				{
					continue;
				}
				InsertGrouped(cachedEntries, new ColonistBar.Entry(pawn, pawn.Map, GroupFor(cachedEntries, pawn.Map)));
				flag = true;
				SquadBar.Anchors(squad, cachedEntries, out first, out second);
				if (first == null)
				{
					continue;
				}
			}
			for (int num = cachedEntries.Count - 1; num >= 0; num--)
			{
				Pawn pawn2 = cachedEntries[num].pawn;
				if (pawn2 != null && pawn2 != first && pawn2 != second && squad.Contains(pawn2))
				{
					cachedEntries.RemoveAt(num);
					flag = true;
				}
			}
			if (second != null)
			{
				flag |= PullTogether(cachedEntries, first, second);
			}
		}
		if (!flag)
		{
			return;
		}
		__instance.cachedReorderableGroups.Clear();
		for (int j = 0; j < cachedEntries.Count; j++)
		{
			__instance.cachedReorderableGroups.Add(-1);
		}
		int num2 = 0;
		int num3 = int.MinValue;
		for (int k = 0; k < cachedEntries.Count; k++)
		{
			if (cachedEntries[k].group != num3)
			{
				num3 = cachedEntries[k].group;
				num2++;
			}
		}
		__instance.drawer.Notify_RecachedEntries();
		__instance.drawLocsFinder.CalculateDrawLocs(__instance.cachedDrawLocs, out __instance.cachedScale, num2);
	}

	private static Pawn FirstDisplayable(Squad squad)
	{
		List<Pawn> list = SquadBar.Ordered(squad);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Spawned && list[i].Map != null)
			{
				return list[i];
			}
		}
		return null;
	}

	private static int GroupFor(List<ColonistBar.Entry> entries, Map map)
	{
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].map == map)
			{
				return entries[i].group;
			}
		}
		return 0;
	}

	private static void InsertGrouped(List<ColonistBar.Entry> entries, ColonistBar.Entry entry)
	{
		int index = entries.Count;
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].group == entry.group)
			{
				index = i + 1;
			}
		}
		entries.Insert(index, entry);
	}

	private static bool PullTogether(List<ColonistBar.Entry> entries, Pawn first, Pawn second)
	{
		int num = entries.FindIndex((ColonistBar.Entry e) => e.pawn == first);
		int num2 = entries.FindIndex((ColonistBar.Entry e) => e.pawn == second);
		if (num < 0 || num2 < 0 || num2 == num + 1)
		{
			return false;
		}
		if (entries[num].group != entries[num2].group)
		{
			return false;
		}
		ColonistBar.Entry item = entries[num2];
		entries.RemoveAt(num2);
		entries.Insert((num2 < num) ? num : (num + 1), item);
		return true;
	}
}
