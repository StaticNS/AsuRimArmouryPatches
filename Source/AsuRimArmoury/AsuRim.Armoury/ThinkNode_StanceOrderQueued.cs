using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class ThinkNode_StanceOrderQueued : ThinkNode
{
	public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
	{
		JobQueue jobQueue = pawn.jobs?.jobQueue;
		if (jobQueue == null || jobQueue.Count == 0)
		{
			return ThinkResult.NoJob;
		}
		if (pawn.Drafted)
		{
			Job curJob = pawn.CurJob;
			if (curJob == null || !curJob.playerForced)
			{
				if (StanceMovement.StanceOf(pawn) == CombatStance.Hold)
				{
					return ThinkResult.NoJob;
				}
				QueuedJob queuedJob = jobQueue.Peek();
				if (queuedJob.job == null || !queuedJob.job.playerForced || !queuedJob.job.CanBeginNow(pawn))
				{
					return ThinkResult.NoJob;
				}
				jobQueue.Dequeue();
				return new ThinkResult(queuedJob.job, this, queuedJob.tag, fromQueue: true);
			}
		}
		return ThinkResult.NoJob;
	}
}
