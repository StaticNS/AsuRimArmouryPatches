using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(AttackTargetFinder), "GetAvailableShootingTargetsByScore")]
internal static class Patch_AvailableShootingTargets
{
	private static bool Prefix(List<IAttackTarget> rawTargets, IAttackTargetSearcher searcher, Verb verb, List<Pair<IAttackTarget, float>> ___availableShootingTargets, ref List<Pair<IAttackTarget, float>> __result)
	{
		if (verb == null || rawTargets == null || !TargetScoring.Applies(searcher))
		{
			return true;
		}
		__result = TargetScoring.ScoreAll(rawTargets, searcher, verb, ___availableShootingTargets);
		return false;
	}
}
