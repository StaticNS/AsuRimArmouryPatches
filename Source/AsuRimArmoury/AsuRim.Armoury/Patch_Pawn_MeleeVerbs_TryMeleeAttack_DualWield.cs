using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_MeleeVerbs), "TryMeleeAttack")]
internal static class Patch_Pawn_MeleeVerbs_TryMeleeAttack_DualWield
{
	private static bool Postfix(bool __result, Pawn_MeleeVerbs __instance, Thing target)
	{
		Pawn pawn = __instance.pawn;
		if (!ArmouryTracked.OffHand.Contains(pawn) || !ArmourySettings.dualWieldEnabled)
		{
			return __result;
		}
		if (ArsenalRules.HoldsGunInRangedStance(pawn))
		{
			return __result;
		}
		if (pawn == null || Arsenal.OffHand(pawn) == null)
		{
			return __result;
		}
		return DualWield.TryStartOffHandMelee(pawn, target) | __result;
	}
}
