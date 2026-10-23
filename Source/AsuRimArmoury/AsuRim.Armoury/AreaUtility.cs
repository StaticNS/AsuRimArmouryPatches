using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

internal static class AreaUtility
{
	private static readonly Dictionary<Region, IntVec3> bestByRegion = new Dictionary<Region, IntVec3>();

	private static readonly List<IntVec3> ordered = new List<IntVec3>();

	internal static IntVec3 ClosestCellIn(Area area, Pawn pawn)
	{
		Map map = pawn?.Map;
		if (area == null || map == null)
		{
			return IntVec3.Invalid;
		}
		IntVec3 origin = pawn.Position;
		bestByRegion.Clear();
		ordered.Clear();
		try
		{
			foreach (IntVec3 activeCell in area.ActiveCells)
			{
				if (activeCell.Standable(map))
				{
					Region region = activeCell.GetRegion(map);
					if (region != null && (!bestByRegion.TryGetValue(region, out var value) || value.DistanceToSquared(origin) > activeCell.DistanceToSquared(origin)))
					{
						bestByRegion[region] = activeCell;
					}
				}
			}
			ordered.AddRange(bestByRegion.Values);
			ordered.Sort((IntVec3 a, IntVec3 b) => a.DistanceToSquared(origin).CompareTo(b.DistanceToSquared(origin)));
			for (int num = 0; num < ordered.Count; num++)
			{
				if (pawn.CanReach(ordered[num], PathEndMode.OnCell, Danger.Deadly))
				{
					return ordered[num];
				}
			}
			return IntVec3.Invalid;
		}
		finally
		{
			bestByRegion.Clear();
			ordered.Clear();
		}
	}
}
