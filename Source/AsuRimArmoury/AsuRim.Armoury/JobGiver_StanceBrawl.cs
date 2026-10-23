using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobGiver_StanceBrawl : JobGiver_StanceBase
{
	public JobGiver_StanceBrawl()
	{
		targetAcquireRadius = 30f;
		targetKeepRadius = 38f;
		chaseTarget = true;
	}

	public override Job TryGiveJob(Pawn pawn)
	{
		RangeDirector.SyncBrawl(pawn);
		return base.TryGiveJob(pawn);
	}

	public override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
	{
		dest = IntVec3.Invalid;
		return false;
	}
}
