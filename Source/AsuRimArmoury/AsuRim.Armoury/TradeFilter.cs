using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

internal static class TradeFilter
{
	internal static IEnumerable<Thing> Without(IEnumerable<Thing> things, Pawn negotiator)
	{
		if (things == null)
		{
			return things;
		}
		List<Thing> list = new List<Thing>();
		foreach (Thing thing in things)
		{
			Pawn pawn = (thing.ParentHolder as Pawn_InventoryTracker)?.pawn;
			if (pawn == null || !SidearmSafety.Protected(pawn, thing))
			{
				list.Add(thing);
			}
		}
		return list;
	}
}
