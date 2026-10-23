using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobDriver_Ambush : JobDriver
{
	private const int WatchInterval = 15;

	private IntVec3 Spot => job.targetA.Cell;

	private Thing Target => job.targetB.Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
		Toil toil = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
		toil.AddPreTickIntervalAction((int delta) =>
		{
			if (pawn.IsHashIntervalTick(15, delta) && JobGiver_StanceHide.ShouldStrike(pawn))
			{
				EndJobWith(JobCondition.InterruptForced);
			}
		});
		yield return toil;
		Toil toil2 = ToilMaker.MakeToil("Ambush");
		toil2.defaultCompleteMode = ToilCompleteMode.Never;
		toil2.handlingFacing = true;
		toil2.initAction = () =>
		{
			Thing target = Target;
			if (target != null)
			{
				pawn.rotationTracker.FaceCell(target.Position);
			}
		};
		toil2.tickIntervalAction = (int delta) =>
		{
			if (pawn.IsHashIntervalTick(15, delta))
			{
				Thing target = Target;
				CompArsenal compArsenal = CompArsenal.For(pawn);
				if (target == null || !target.Spawned || target.Destroyed || target.Map != pawn.Map || compArsenal == null || compArsenal.ambushTarget != target || JobGiver_StanceHide.ShouldStrike(pawn))
				{
					EndJobWith(JobCondition.InterruptForced);
				}
			}
		};
		yield return toil2;
	}
}
