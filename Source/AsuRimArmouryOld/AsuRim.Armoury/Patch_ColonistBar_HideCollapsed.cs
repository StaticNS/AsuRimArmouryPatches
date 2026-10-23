using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
internal static class Patch_ColonistBar_HideCollapsed
{
	private static int processedVersion = -1;

	private static ColonistBar processedBar;

	private static readonly FieldInfo entriesDirtyField = AccessTools.Field(typeof(ColonistBar), "entriesDirty");

	private static readonly FieldInfo cachedReorderableGroupsField = AccessTools.Field(typeof(ColonistBar), "cachedReorderableGroups");

	private static readonly FieldInfo drawLocsFinderField = AccessTools.Field(typeof(ColonistBar), "drawLocsFinder");

	private static readonly FieldInfo cachedDrawLocsField = AccessTools.Field(typeof(ColonistBar), "cachedDrawLocs");

	private static readonly FieldInfo cachedScaleField = AccessTools.Field(typeof(ColonistBar), "cachedScale");

	private static void Prefix(ColonistBar __instance, out bool __state)
	{
		__state = (bool)entriesDirtyField.GetValue(__instance);
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
			SquadBar.RecacheAnchors(__instance.Entries, null);
			return;
		}
		try
		{
			Process(__instance, current);
		}
		finally
		{
			SquadBar.RecacheAnchors(__instance.Entries, current.Squads);
		}
	}

	private static void Process(ColonistBar __instance, SquadManager manager)
	{
		List<ColonistBar.Entry> cachedEntries = __instance.Entries;
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
		List<int> cachedReorderableGroups = (List<int>)cachedReorderableGroupsField.GetValue(__instance);
		cachedReorderableGroups.Clear();
		for (int j = 0; j < cachedEntries.Count; j++)
		{
			cachedReorderableGroups.Add(-1);
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
		object drawLocsFinder = drawLocsFinderField.GetValue(__instance);
		List<Vector2> cachedDrawLocs = (List<Vector2>)cachedDrawLocsField.GetValue(__instance);
		object[] arguments = { cachedDrawLocs, cachedScaleField.GetValue(__instance), num2 };
		MethodInfo calculateDrawLocs = AccessTools.Method(drawLocsFinder.GetType(), "CalculateDrawLocs", new[]
		{
			typeof(List<Vector2>),
			typeof(float).MakeByRefType(),
			typeof(int)
		});
		calculateDrawLocs.Invoke(drawLocsFinder, arguments);
		cachedScaleField.SetValue(__instance, arguments[1]);
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
