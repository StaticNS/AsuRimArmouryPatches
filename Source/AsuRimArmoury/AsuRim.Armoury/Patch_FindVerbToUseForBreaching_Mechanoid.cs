using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(BreachingUtility), "FindVerbToUseForBreaching")]
internal static class Patch_FindVerbToUseForBreaching_Mechanoid
{
	private static void Postfix(Pawn pawn, ref Verb __result)
	{
		if (__result == null && pawn?.RaceProps != null && pawn.RaceProps.IsMechanoid && pawn.meleeVerbs != null)
		{
			Verb verb = pawn.meleeVerbs.TryGetMeleeVerb(pawn);
			if (verb != null && verb.Available() && verb.HarmsHealth())
			{
				__result = verb;
			}
		}
	}
}
