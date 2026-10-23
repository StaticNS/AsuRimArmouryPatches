using Verse;

namespace AsuRim.Armoury;

internal static class SidearmSafety
{
	internal static bool Protected(Pawn pawn, Thing thing)
	{
		if (thing == null || pawn == null)
		{
			return false;
		}
		if (!thing.def.IsRangedWeapon && !thing.def.IsMeleeWeapon)
		{
			return false;
		}
		return LoadoutCoverage.Covers(pawn, thing);
	}
}
