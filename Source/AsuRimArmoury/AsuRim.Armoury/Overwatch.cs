using Verse;

namespace AsuRim.Armoury;

public static class Overwatch
{
	public static bool IsHolding(Pawn pawn, out Area zone)
	{
		zone = null;
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null || !compArsenal.overwatch || !ArmourySettings.stancesEnabled)
		{
			return false;
		}
		zone = compArsenal.OverwatchArea;
		return zone != null;
	}

	public static bool Triggers(Area zone, Thing target)
	{
		if (target != null && target.Spawned)
		{
			return zone[target.Position];
		}
		return false;
	}
}
