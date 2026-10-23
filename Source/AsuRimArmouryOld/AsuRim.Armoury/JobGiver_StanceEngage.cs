using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobGiver_StanceEngage : JobGiver_StanceBase
{
	public JobGiver_StanceEngage()
	{
		targetAcquireRadius = 45f;
		targetKeepRadius = 55f;
		chaseTarget = false;
	}

	protected override Job PriorityJob(Pawn pawn)
	{
		return StanceKiting.TryKite(pawn);
	}

	protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
	{
		Thing enemyTarget = pawn.mindState.enemyTarget;
		Verb verb = verbToUse ?? StanceMovement.AttackVerb(pawn, enemyTarget, allowTurrets);
		if (verb == null || verb.verbProps.IsMeleeAttack)
		{
			dest = IntVec3.Invalid;
			return false;
		}
		if (StanceMovement.TryFindFiringCell(pawn, enemyTarget, verb, StanceMovement.Standoff(CombatStance.Engage, verb.EffectiveRange), out dest))
		{
			return true;
		}
		return StanceMovement.TryFindFiringCell(pawn, enemyTarget, verb, 0f, out dest);
	}
}
