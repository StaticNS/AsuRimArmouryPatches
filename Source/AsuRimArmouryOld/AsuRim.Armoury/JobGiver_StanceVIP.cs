using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;

namespace AsuRim.Armoury;

public class JobGiver_StanceVIP : JobGiver_StanceBase
{
	private static readonly MethodInfo notifyEngagedTarget = AccessTools.Method(typeof(Pawn_MindState), "Notify_EngagedTarget");

	public JobGiver_StanceVIP()
	{
		targetAcquireRadius = 200f;
		targetKeepRadius = 200f;
		chaseTarget = true;
	}

	protected override void UpdateEnemyTarget(Pawn pawn)
	{
		Pawn pawn2 = VIPTracker.Of(pawn.Map);
		if (pawn2 != null && !pawn2.Downed && pawn2.HostileTo(pawn.Faction))
		{
			pawn.mindState.enemyTarget = pawn2;
			notifyEngagedTarget?.Invoke(pawn.mindState, null);
			return;
		}
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
