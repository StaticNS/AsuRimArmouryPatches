using Verse;

namespace AsuRim.Armoury;

public class JobGiver_StanceCovering : JobGiver_StanceBase
{
	private const float CoverRadius = 12f;

	public JobGiver_StanceCovering()
	{
		targetAcquireRadius = 45f;
		targetKeepRadius = 55f;
		chaseTarget = false;
	}

	public override void UpdateEnemyTarget(Pawn pawn)
	{
		Thing thing = StanceTargeting.ThrottledScan(pawn, () => StanceTargeting.NearestHostileForCovering(pawn, 12f));
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
