using Verse;

namespace AsuRim.Armoury;

public class JobGiver_StanceHunt : JobGiver_StanceBase
{
	public JobGiver_StanceHunt()
	{
		targetAcquireRadius = 45f;
		targetKeepRadius = 53f;
		chaseTarget = true;
	}

	public override void UpdateEnemyTarget(Pawn pawn)
	{
		Thing thing = StanceTargeting.ThrottledScan(pawn, () => StanceTargeting.WeakestHostile(pawn, 45f));
		if (thing != null)
		{
			pawn.mindState.enemyTarget = thing;
			pawn.mindState.Notify_EngagedTarget();
		}
		else
		{
			base.UpdateEnemyTarget(pawn);
		}
	}
}
