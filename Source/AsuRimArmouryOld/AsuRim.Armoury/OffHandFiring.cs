using ArmouryTactics.Compatability;
using Verse;

namespace AsuRim.Armoury;

internal static class OffHandFiring
{
	private static Pawn firing;

	internal static bool NativeActive
	{
		get
		{
			if (ArmourySettings.dualWieldEnabled)
			{
				return !CombatExtendedCompat.Active;
			}
			return false;
		}
	}

	internal static void Begin(Pawn pawn)
	{
		firing = pawn;
	}

	internal static void End()
	{
		firing = null;
	}

	internal static bool Active(Pawn pawn)
	{
		return firing == pawn;
	}
}
