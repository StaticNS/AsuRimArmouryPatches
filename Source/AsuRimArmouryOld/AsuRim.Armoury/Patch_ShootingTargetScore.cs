using HarmonyLib;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(AttackTargetFinder), "GetShootingTargetScore")]
internal static class Patch_ShootingTargetScore
{
	private static bool Prefix(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb, ref float __result)
	{
		if (!ArmourySettings.unifiedTargeting || TargetScoring.Busy)
		{
			return true;
		}
		if (target?.Thing == null || searcher?.Thing == null || searcher.Thing.Map == null)
		{
			__result = 0f;
			return false;
		}
		if (!TargetScoring.Applies(searcher))
		{
			return true;
		}
		__result = TargetScoring.ScoreOne(target, searcher, verb);
		return false;
	}
}
