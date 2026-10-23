using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public static class SurveillanceShiftEscort
{
	private const int SyncInterval = 250;

	public static Pawn Desired(Pawn member)
	{
		if (!GuardStandDown.Active || member == null || member.Dead)
		{
			return null;
		}
		Squad squad = SquadManager.Current?.SquadOf(member);
		if (squad == null || !squad.Escorting)
		{
			return null;
		}
		Pawn leader = squad.Leader;
		if (leader == null || leader == member || leader.Dead || leader.Destroyed)
		{
			return null;
		}
		return leader;
	}

	public static void Sync(Pawn member)
	{
		CompArsenal compArsenal = CompArsenal.For(member);
		if (compArsenal == null)
		{
			return;
		}
		Pawn ssEscort = compArsenal.ssEscort;
		Pawn pawn = Desired(member);
		if (ssEscort == pawn)
		{
			return;
		}
		if (!GuardStandDown.Present || !SurveillanceShiftBridge.EscortReady)
		{
			compArsenal.ssEscort = null;
			return;
		}
		try
		{
			if (ssEscort != null && SurveillanceShiftBridge.ProtecteeOf(member) == ssEscort)
			{
				SurveillanceShiftBridge.SetProtectee(member, null);
			}
			if (pawn != null)
			{
				SurveillanceShiftBridge.SetProtectee(member, pawn);
			}
			compArsenal.ssEscort = pawn;
		}
		catch (Exception ex)
		{
			compArsenal.ssEscort = null;
			Log.WarningOnce("[Rules of Engagement] Could not update the Surveillance Shift bodyguard for " + member?.ToString() + ": " + ex.GetBaseException().Message, member.thingIDNumber ^ 0x55E5C0);
		}
	}

	public static void Sync(Squad squad)
	{
		if (squad != null)
		{
			List<Pawn> list = new List<Pawn>(squad.Members);
			for (int i = 0; i < list.Count; i++)
			{
				Sync(list[i]);
			}
		}
	}

	public static void Sync(IEnumerable<Pawn> pawns)
	{
		if (pawns != null)
		{
			List<Pawn> list = new List<Pawn>(pawns);
			for (int i = 0; i < list.Count; i++)
			{
				Sync(list[i]);
			}
		}
	}

	public static void SyncAll()
	{
		SquadManager current = SquadManager.Current;
		if (current != null)
		{
			List<Squad> squads = current.Squads;
			for (int i = 0; i < squads.Count; i++)
			{
				Sync(squads[i]);
			}
		}
		List<Map> maps = Find.Maps;
		if (maps != null)
		{
			for (int j = 0; j < maps.Count; j++)
			{
				Sync(maps[j].mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer));
			}
		}
	}

	public static void Tick(int ticksGame)
	{
		if (GuardStandDown.Present && ticksGame % 250 == 0)
		{
			SyncAll();
		}
	}
}
