using HarmonyLib;
using Verse;
using Verse.AI;
using System.Reflection;

namespace AsuRim.Armoury;

public class JobGiver_StanceHunt : JobGiver_StanceBase
{
	private static readonly MethodInfo notifyEngagedTarget = AccessTools.Method(typeof(Pawn_MindState), "Notify_EngagedTarget");

	public JobGiver_StanceHunt()
	{
		targetAcquireRadius = 45f;
		targetKeepRadius = 53f;
		chaseTarget = true;
	}

	protected override void UpdateEnemyTarget(Pawn pawn)
	{
		Thing thing = StanceTargeting.ThrottledScan(pawn, () => StanceTargeting.WeakestHostile(pawn, 45f));
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
