using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn), "CurrentEffectiveVerb", MethodType.Getter)]
internal static class Patch_Pawn_CurrentEffectiveVerb_Brawl
{
	[HarmonyPriority(0)]
	private static void Postfix(Pawn __instance, ref Verb __result)
	{
		__result = BrawlMeleeOnly.Filter(__instance, __instance.mindState?.enemyTarget, __result);
	}
}
