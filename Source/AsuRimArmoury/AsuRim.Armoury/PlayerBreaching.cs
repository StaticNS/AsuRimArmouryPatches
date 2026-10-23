using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AsuRim.Armoury;

public static class PlayerBreaching
{
	public static IntVec3 FindBreachTarget(Pawn pawn)
	{
		Map map = pawn?.Map;
		if (map == null)
		{
			return IntVec3.Invalid;
		}
		Building building = null;
		float num = float.MaxValue;
		List<Building> allBuildingsNonColonist = map.listerBuildings.allBuildingsNonColonist;
		for (int i = 0; i < allBuildingsNonColonist.Count; i++)
		{
			Building building2 = allBuildingsNonColonist[i];
			if (building2 == null)
			{
				continue;
			}
			bool flag = building2.Faction != null && building2.Faction.HostileTo(pawn.Faction);
			bool flag2 = !flag && building2.Faction == null && building2.def.passability == Traversability.Impassable && BreachingUtility.ShouldBreachBuilding(building2);
			if ((!flag && !flag2) || (flag && (!building2.def.IsEdifice() || building2.def.passability != Traversability.Impassable) && !(building2 is Building_Door)))
			{
				continue;
			}
			float num2 = pawn.Position.DistanceTo(building2.Position);
			if (!(num2 > 120f))
			{
				float num3 = building2.MaxHitPoints;
				if (building2 is Building_Door)
				{
					num3 *= 0.35f;
				}
				float num4 = num3 + num2 * 12f;
				if (num4 < num)
				{
					num = num4;
					building = building2;
				}
			}
		}
		if (building == null)
		{
			return IntVec3.Invalid;
		}
		IntVec3 intVec = building.Position - pawn.Position;
		IntVec3 intVec2 = building.Position + new IntVec3(Mathf.Clamp(intVec.x, -3, 3), 0, Mathf.Clamp(intVec.z, -3, 3));
		if (!intVec2.InBounds(map))
		{
			return building.Position;
		}
		return intVec2;
	}

	public static void AssignTo(Pawn pawn)
	{
		if (pawn?.Map == null)
		{
			return;
		}
		IntVec3 target = FindBreachTarget(pawn);
		if (target.IsValid)
		{
			Lord lord = pawn.GetLord();
			if (!(lord?.LordJob is LordJob_PlayerBreach))
			{
				lord?.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
				LordMaker.MakeNewLord(pawn.Faction, new LordJob_PlayerBreach(target), pawn.Map, new List<Pawn> { pawn });
			}
		}
	}

	public static void Release(Pawn pawn)
	{
		Lord lord = pawn?.GetLord();
		if (lord?.LordJob is LordJob_PlayerBreach)
		{
			lord.Notify_PawnLost(pawn, PawnLostCondition.Undefined);
		}
	}
}
