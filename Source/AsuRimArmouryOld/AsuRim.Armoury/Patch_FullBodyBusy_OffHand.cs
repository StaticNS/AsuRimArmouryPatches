using AsuRimArmoury.Kit;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_StanceTracker), "FullBodyBusy", MethodType.Getter)]
internal static class Patch_FullBodyBusy_OffHand
{
	private static bool fired;

	private static void Postfix(Pawn_StanceTracker __instance, ref bool __result)
	{
		if (!fired)
		{
			fired = PatchGuard.Fired("Patch_FullBodyBusy_OffHand");
		}
		if (!__result && ArmouryTracked.OffHand.Contains(__instance.pawn) && DualWield.OffHandBusy(__instance.pawn) && !DualWield.WalkFiring(__instance.pawn))
		{
			__result = true;
		}
	}
}
