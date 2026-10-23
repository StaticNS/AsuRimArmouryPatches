using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn), "TryGetAttackVerb")]
internal static class Patch_Pawn_TryGetAttackVerb_Brawl
{
	[HarmonyPriority(0)]
	private static void Postfix(Pawn __instance, Thing target, ref Verb __result)
	{
		__result = BrawlMeleeOnly.Filter(__instance, target, __result);
	}
}
