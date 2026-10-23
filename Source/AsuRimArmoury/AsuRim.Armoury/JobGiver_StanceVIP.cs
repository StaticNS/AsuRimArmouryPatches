using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public class JobGiver_StanceVIP : JobGiver_StanceBase
{
	public JobGiver_StanceVIP()
	{
		targetAcquireRadius = 200f;
		targetKeepRadius = 200f;
		chaseTarget = true;
	}

	public override void UpdateEnemyTarget(Pawn pawn)
	{
		Pawn pawn2 = VIPTracker.Of(pawn.Map);
		if (pawn2 != null && !pawn2.Downed && pawn2.HostileTo(pawn.Faction))
		{
			pawn.mindState.enemyTarget = pawn2;
			pawn.mindState.Notify_EngagedTarget();
			return;
		}
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
