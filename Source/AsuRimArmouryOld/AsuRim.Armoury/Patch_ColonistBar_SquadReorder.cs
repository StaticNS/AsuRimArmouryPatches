using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(ColonistBar), "Reorder")]
internal static class Patch_ColonistBar_SquadReorder
{
	private static Pawn dragged;

	private static void Prefix(ColonistBar __instance, int from, int entryGroup)
	{
		dragged = null;
		List<ColonistBar.Entry> cachedEntries = __instance.Entries;
		int num = 0;
		for (int i = 0; i < cachedEntries.Count; i++)
		{
			if (cachedEntries[i].group == entryGroup && cachedEntries[i].pawn != null)
			{
				if (num == from)
				{
					dragged = cachedEntries[i].pawn;
					break;
				}
				num++;
			}
		}
	}

	private static void Postfix(ColonistBar __instance)
	{
		SquadManager current = SquadManager.Current;
		if (current == null)
		{
			return;
		}
		List<ColonistBar.Entry> cachedEntries = __instance.Entries;
		Squad squad = ((dragged?.playerSettings != null) ? current.SquadOf(dragged) : null);
		if (squad != null && squad.collapsed)
		{
			Flatten(squad, dragged.playerSettings.displayOrder);
		}
		dragged = null;
		List<Squad> squads = current.Squads;
		for (int i = 0; i < squads.Count; i++)
		{
			Squad squad2 = squads[i];
			if (!squad2.collapsed)
			{
				continue;
			}
			int num = int.MaxValue;
			List<Pawn> members = squad2.Members;
			for (int j = 0; j < members.Count; j++)
			{
				if (InEntries(cachedEntries, members[j]))
				{
					int num2 = members[j].playerSettings?.displayOrder ?? int.MaxValue;
					if (num2 < num)
					{
						num = num2;
					}
				}
			}
			if (num == int.MaxValue)
			{
				continue;
			}
			for (int k = 0; k < members.Count; k++)
			{
				if (!InEntries(cachedEntries, members[k]) && members[k].playerSettings != null)
				{
					members[k].playerSettings.displayOrder = num;
				}
			}
		}
	}

	private static void Flatten(Squad squad, int order)
	{
		List<Pawn> members = squad.Members;
		for (int i = 0; i < members.Count; i++)
		{
			if (members[i].playerSettings != null)
			{
				members[i].playerSettings.displayOrder = order;
			}
		}
	}

	private static bool InEntries(List<ColonistBar.Entry> entries, Pawn pawn)
	{
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].pawn == pawn)
			{
				return true;
			}
		}
		return false;
	}
}
