using HarmonyLib;
using Verse;
using Verse.AI;
using System.Reflection;

namespace AsuRim.Armoury;

public class JobGiver_StanceCovering : JobGiver_StanceBase
{
	private const float CoverRadius = 12f;
	private static readonly MethodInfo notifyEngagedTarget = AccessTools.Method(typeof(Pawn_MindState), "Notify_EngagedTarget");

	public JobGiver_StanceCovering()
	{
		targetAcquireRadius = 45f;
		targetKeepRadius = 55f;
		chaseTarget = false;
	}

	protected override void UpdateEnemyTarget(Pawn pawn)
	{
		Thing thing = StanceTargeting.ThrottledScan(pawn, () => StanceTargeting.NearestHostileForCovering(pawn, 12f));
		if (thing != null)
		{
			pawn.mindState.enemyTarget = thing;
			notifyEngagedTarget?.Invoke(pawn.mindState, null);
		}
		else
		{
			base.UpdateEnemyTarget(pawn);
		}
	}
}
