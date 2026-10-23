using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_MeleeVerbs), "TryMeleeAttack")]
internal static class Patch_Pawn_MeleeVerbs_TryMeleeAttack_RangedStance
{
	private static bool Prefix(Pawn_MeleeVerbs __instance, Thing target, ref bool __result)
	{
		Pawn pawn = __instance.Pawn;
		if (!ArsenalRules.HoldsGunInRangedStance(pawn))
		{
			return true;
		}
		__result = target != null && pawn.TryStartAttack(target);
		return false;
	}
}
