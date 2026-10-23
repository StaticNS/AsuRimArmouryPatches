using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(LordToil_AssaultColonyBreaching), "UpdateCurrentBreachTarget")]
internal static class Patch_UpdateCurrentBreachTarget_PreferWeakest
{
	private static void Postfix(LordToil_AssaultColonyBreaching __instance)
	{
		if (!(__instance.lord?.LordJob is LordJob_PlayerBreach))
		{
			return;
		}
		LordToilData_AssaultColonyBreaching data = __instance.Data;
		if (data?.currentTarget != null && data.breachingGrid != null)
		{
			Thing thing = FindWeakestBreachTarget(data.breachingGrid);
			if (thing != null)
			{
				data.currentTarget = thing;
			}
		}
	}

	private static Thing FindWeakestBreachTarget(BreachingGrid grid)
	{
		Map map = grid.Map;
		Building result = null;
		bool flag = true;
		float num = float.MaxValue;
		foreach (IntVec3 activeCell in grid.BreachGrid.ActiveCells)
		{
			List<Thing> thingList = activeCell.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++)
			{
				if (thingList[i] is Building building && BreachingUtility.ShouldBreachBuilding(building) && BreachingUtility.IsWorthBreachingBuilding(grid, building) && BreachingUtility.CountReachableAdjacentCells(grid, building) > 0)
				{
					bool mineable = building.def.mineable;
					float num2 = building.HitPoints;
					bool flag2 = flag && !mineable;
					if (!flag2 && mineable == flag)
					{
						flag2 = num2 < num;
					}
					if (flag2)
					{
						result = building;
						flag = mineable;
						num = num2;
					}
				}
			}
		}
		return result;
	}
}
