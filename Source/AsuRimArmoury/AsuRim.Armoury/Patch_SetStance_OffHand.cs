using ArmouryTactics;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_StanceTracker), "SetStance")]
internal static class Patch_SetStance_OffHand
{
	private static bool Prepare()
	{
		RunAndGunStance.Configure();
		return true;
	}

	private static bool Prefix(Pawn_StanceTracker __instance, ref Stance newStance)
	{
		if (newStance == null)
		{
			return true;
		}
		if (RunAndGunStance.Active && RunAndGunStance.Intercept(__instance, ref newStance))
		{
			return false;
		}
		Pawn pawn = __instance.pawn;
		if (!OffHandFiring.Active(pawn))
		{
			return true;
		}
		Pawn_StanceTracker pawn_StanceTracker = DualWield.OffHandStances(pawn);
		if (pawn_StanceTracker == null || __instance == pawn_StanceTracker)
		{
			return true;
		}
		pawn_StanceTracker.SetStance(newStance);
		return false;
	}
}
