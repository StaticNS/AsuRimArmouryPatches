using AsuRimArmoury.Kit;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn), "CurrentEffectiveVerb", MethodType.Getter)]
internal static class Patch_Pawn_CurrentEffectiveVerb_DualWield
{
	private static bool fired;

	private static void Postfix(Pawn __instance, ref Verb __result)
	{
		if (!fired)
		{
			fired = PatchGuard.Fired("Patch_Pawn_CurrentEffectiveVerb_DualWield");
		}
		__result = DualWield.EffectiveVerbFor(__instance, __instance.mindState?.enemyTarget, __result);
	}
}
