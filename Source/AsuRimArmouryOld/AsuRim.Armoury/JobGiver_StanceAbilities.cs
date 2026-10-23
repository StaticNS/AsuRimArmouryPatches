using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobGiver_StanceAbilities : ThinkNode_JobGiver
{
	private const float MeleeEngageRadius = 8f;

	protected override Job TryGiveJob(Pawn pawn)
	{
		Pawn_AbilityTracker pawn_AbilityTracker = pawn?.abilities;
		if (pawn_AbilityTracker == null || pawn.Map == null)
		{
			return null;
		}
		if (pawn.carryTracker?.CarriedThing != null)
		{
			return null;
		}
		List<Ability> allAbilitiesForReading = pawn_AbilityTracker.AllAbilitiesForReading;
		if (allAbilitiesForReading.NullOrEmpty())
		{
			return null;
		}
		Thing thing = CurrentEnemy(pawn);
		int num = -1;
		for (int i = 0; i < allAbilitiesForReading.Count; i++)
		{
			Ability ability = allAbilitiesForReading[i];
			if (!Castable(pawn, ability))
			{
				continue;
			}
			AbilityProfile abilityProfile = AbilityProfile.Of(ability.def);
			if (abilityProfile.HarmsCaster)
			{
				continue;
			}
			LocalTargetInfo localTargetInfo;
			if (SelfCast(ability))
			{
				if (abilityProfile.BuffActive(pawn))
				{
					continue;
				}
				if (num < 0)
				{
					num = (EnemyInEngagementRange(pawn, thing) ? 1 : 0);
				}
				if (num == 0 || !ability.AICanTargetNow(pawn))
				{
					continue;
				}
				localTargetInfo = pawn;
			}
			else
			{
				if (thing == null || !ability.def.ai_IsOffensive || ability.verb == null || !ability.def.verbProperties.targetParams.CanTarget(thing) || !ability.AICanTargetNow(thing) || !ability.verb.CanHitTarget(thing))
				{
					continue;
				}
				localTargetInfo = thing;
			}
			Job job = ability.GetJob(localTargetInfo, localTargetInfo);
			if (job != null)
			{
				job.expiryInterval = 60;
				job.checkOverrideOnExpire = true;
				return job;
			}
		}
		return null;
	}

	private static bool Castable(Pawn pawn, Ability ability)
	{
		if (ability?.def == null || !ability.def.aiCanUse)
		{
			return false;
		}
		if (!ability.CanCast.Accepted)
		{
			return false;
		}
		if (ability.GizmoDisabled(out var _))
		{
			return false;
		}
		if (ability is Psycast && pawn.psychicEntropy != null && ability.def.EntropyGain > float.Epsilon)
		{
			Pawn_PsychicEntropyTracker psychicEntropy = pawn.psychicEntropy;
			float num = ability.def.EntropyGain * pawn.GetStatValue(StatDefOf.PsychicEntropyGain);
			if (psychicEntropy.EntropyValue + num > psychicEntropy.MaxEntropy)
			{
				return false;
			}
		}
		return true;
	}

	private static bool SelfCast(Ability ability)
	{
		VerbProperties verbProperties = ability.def.verbProperties;
		if (verbProperties == null)
		{
			return false;
		}
		if (verbProperties.targetParams != null && verbProperties.targetParams.canTargetSelf)
		{
			if (verbProperties.targetable)
			{
				return !ability.def.targetRequired;
			}
			return true;
		}
		return false;
	}

	private static Thing CurrentEnemy(Pawn pawn)
	{
		Thing thing = pawn.mindState?.enemyTarget;
		if (thing == null || !thing.Spawned || thing.Destroyed || thing.Map != pawn.Map)
		{
			return null;
		}
		if (thing is Pawn pawn2 && (pawn2.Dead || pawn2.Downed || !pawn2.HostileTo(pawn)))
		{
			return null;
		}
		return thing;
	}

	private static float EngagementRange(Pawn pawn)
	{
		if (StanceMovement.HoldsGun(pawn))
		{
			Verb verb = pawn.equipment.PrimaryEq?.PrimaryVerb;
			if (verb != null && !verb.verbProps.IsMeleeAttack)
			{
				return verb.EffectiveRange;
			}
		}
		return 8f;
	}

	private static bool EnemyInEngagementRange(Pawn pawn, Thing enemy)
	{
		Map map = pawn.Map;
		float num = EngagementRange(pawn);
		float num2 = num * num;
		if (enemy != null && (float)pawn.Position.DistanceToSquared(enemy.Position) <= num2 && GenSight.LineOfSight(pawn.Position, enemy.Position, map, skipFirstCell: true))
		{
			return true;
		}
		List<Pawn> list = StanceTargeting.HostilePawns(map, pawn.Faction);
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn2 = list[i];
			if (!pawn2.Position.Fogged(map) && !((float)pawn.Position.DistanceToSquared(pawn2.Position) > num2) && GenSight.LineOfSight(pawn.Position, pawn2.Position, map, skipFirstCell: true))
			{
				return true;
			}
		}
		return false;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		return base.DeepCopy(resolve);
	}
}
