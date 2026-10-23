using HarmonyLib;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(AttackTargetFinder), "BestAttackTarget")]
internal static class Patch_BestAttackTarget_Overwatch
{
	private static void Postfix(ref IAttackTarget __result, IAttackTargetSearcher searcher)
	{
		if (__result != null && searcher is Pawn pawn && Overwatch.IsHolding(pawn, out var zone) && !Overwatch.Triggers(zone, __result.Thing))
		{
			__result = null;
		}
	}
}
