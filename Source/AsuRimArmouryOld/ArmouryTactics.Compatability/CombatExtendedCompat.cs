using Verse;

namespace ArmouryTactics.Compatability;

internal static class CombatExtendedCompat
{
	private static bool? active;

	internal static bool Active => (active ?? (active = ModsConfig.IsActive("CETeam.CombatExtended"))).Value;
}
