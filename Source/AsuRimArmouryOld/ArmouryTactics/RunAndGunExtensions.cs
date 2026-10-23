using AsuRim.Armoury;
using Verse;

namespace ArmouryTactics;

internal static class RunAndGunExtensions
{
	public static bool RunsAndGuns(this Pawn pawn)
	{
		return CompArsenal.For(pawn)?.walkFire ?? false;
	}

	public static void SetRunsAndGuns(this Pawn pawn, bool set)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null)
		{
			compArsenal.walkFire = set;
		}
	}
}
