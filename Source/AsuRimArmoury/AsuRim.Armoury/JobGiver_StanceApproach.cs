using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobGiver_StanceApproach : ThinkNode_JobGiver
{
	private const int ExpiryTicks = 60;

	private const int MaxMeleeTests = 6;

	private const int MaxFiringTests = 3;

	private static readonly List<Thing> candidates = new List<Thing>();

	public override Job TryGiveJob(Pawn pawn)
	{
		Map map = pawn?.Map;
		if (map == null)
		{
			return null;
		}
		CombatStance stance = StanceMovement.StanceOf(pawn);
		float num = StanceMovement.SearchRadius(stance);
		bool flag = stance.WantsMelee() || !StanceMovement.HoldsGun(pawn);
		Verb verb = (flag ? null : StanceMovement.AttackVerb(pawn, null, allowTurrets: false));
		if (!flag && (verb == null || verb.verbProps.IsMeleeAttack))
		{
			return null;
		}
		Area area = StanceMovement.FenceArea(pawn);
		Pawn charge = StanceMovement.FenceCharge(pawn);
		candidates.Clear();
		float num2 = num * num;
		List<IAttackTarget> potentialTargetsFor = map.attackTargetsCache.GetPotentialTargetsFor(pawn);
		for (int i = 0; i < potentialTargetsFor.Count; i++)
		{
			IAttackTarget attackTarget = potentialTargetsFor[i];
			Thing thing = attackTarget?.Thing;
			if (thing != null && thing.Spawned && !attackTarget.ThreatDisabled(pawn) && AttackTargetFinder.IsAutoTargetable(attackTarget) && (!(thing is Pawn pawn2) || (!pawn2.Downed && pawn2.IsCombatant() && !StanceTargeting.PassiveAnimal(pawn2))) && (pawn.Faction == null || !pawn.Faction.IsPlayer || !thing.Position.Fogged(map)) && !((float)pawn.Position.DistanceToSquared(thing.Position) > num2) && (area == null || area[thing.Position]))
			{
				candidates.Add(thing);
			}
		}
		if (candidates.Count == 0)
		{
			return null;
		}
		IntVec3 from = pawn.Position;
		candidates.Sort((Thing a, Thing b) => a.Position.DistanceToSquared(from).CompareTo(b.Position.DistanceToSquared(from)));
		try
		{
			return flag ? MeleeApproach(pawn, charge) : FiringApproach(pawn, stance, verb);
		}
		finally
		{
			candidates.Clear();
		}
	}

	private static Job MeleeApproach(Pawn pawn, Pawn charge)
	{
		int num = 0;
		for (int i = 0; i < candidates.Count; i++)
		{
			if (num >= 6)
			{
				break;
			}
			Thing thing = candidates[i];
			if (charge != null && !thing.Position.InHorDistOf(charge.Position, 8.5f))
			{
				continue;
			}
			num++;
			if (pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly))
			{
				if (pawn.CanReachImmediate(thing, PathEndMode.Touch))
				{
					return null;
				}
				Job job = JobMaker.MakeJob(JobDefOf.Goto, thing);
				job.expiryInterval = 60;
				job.checkOverrideOnExpire = true;
				job.collideWithPawns = true;
				return job;
			}
		}
		return null;
	}

	private static Job FiringApproach(Pawn pawn, CombatStance stance, Verb verb)
	{
		float standoff = StanceMovement.Standoff(stance, verb.EffectiveRange);
		int num = 0;
		for (int i = 0; i < candidates.Count; i++)
		{
			if (num >= 3)
			{
				break;
			}
			Thing target = candidates[i];
			num++;
			if (StanceMovement.TryFindFiringCell(pawn, target, verb, standoff, out var dest) || StanceMovement.TryFindFiringCell(pawn, target, verb, 0f, out dest))
			{
				if (dest == pawn.Position)
				{
					return null;
				}
				Job job = JobMaker.MakeJob(JobDefOf.Goto, dest);
				job.expiryInterval = 60;
				job.checkOverrideOnExpire = true;
				return job;
			}
		}
		return null;
	}
}
