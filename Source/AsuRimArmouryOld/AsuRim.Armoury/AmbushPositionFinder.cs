using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class AmbushPositionFinder
{
	private const float MaxOffsetFromPost = 12f;

	public static bool TryFindHiddenCell(Pawn pawn, Thing threat, IntVec3 post, out IntVec3 result)
	{
		result = IntVec3.Invalid;
		if (pawn?.Map == null || threat == null)
		{
			return false;
		}
		Map map = pawn.Map;
		IntVec3 position = threat.Position;
		float num = float.MinValue;
		foreach (IntVec3 item in GenRadial.RadialCellsAround(post, 12f, useCenter: true))
		{
			if (item.InBounds(map) && item.Standable(map) && !GenSight.LineOfSight(position, item, map, skipFirstCell: true) && pawn.CanReach(item, PathEndMode.OnCell, Danger.Deadly))
			{
				float num2 = -item.DistanceToSquared(post);
				if (num2 > num)
				{
					num = num2;
					result = item;
				}
			}
		}
		return result.IsValid;
	}
}
