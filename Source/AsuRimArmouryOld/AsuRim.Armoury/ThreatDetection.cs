using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

internal static class ThreatDetection
{
	private const float MeleeRange = 8f;

	private const float MinRangedRange = 2f;

	private const float MaxRangedRange = 20f;

	private const float RangedFraction = 0.66f;

	internal static bool AnyWithin(Pawn pawn, float distance)
	{
		List<IAttackTarget> potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
		float num = distance * distance;
		IntVec3 position = pawn.Position;
		for (int i = 0; i < potentialTargetsFor.Count; i++)
		{
			Thing thing = potentialTargetsFor[i].Thing;
			if (thing != null && (float)(thing.Position - position).LengthHorizontalSquared <= num)
			{
				return true;
			}
		}
		return false;
	}

	internal static bool InRange(Pawn pawn, out Thing target)
	{
		target = null;
		if (pawn?.Map == null)
		{
			return false;
		}
		Verb currentEffectiveVerb = pawn.CurrentEffectiveVerb;
		if (currentEffectiveVerb == null)
		{
			return false;
		}
		float num = (currentEffectiveVerb.IsMeleeAttack ? 8f : Mathf.Clamp(currentEffectiveVerb.EffectiveRange * 0.66f, 2f, 20f));
		if (!AnyWithin(pawn, num))
		{
			return false;
		}
		target = (Thing)AttackTargetFinder.BestAttackTarget(pawn, TargetScanFlags.NeedLOSToAll | TargetScanFlags.NeedReachableIfCantHitFromMyPos | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, null, 0f, num);
		return target != null;
	}
}
