using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobDriver_Bodyguard : JobDriver
{
	private const int RecheckInterval = 600;

	private const float SlotTolerance = 1.6f;

	private Pawn Charge => job.targetA.Thing as Pawn;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedOrNull(TargetIndex.A);
		this.FailOn(() => CompArsenal.For(pawn)?.guarding != Charge || !GuardStandDown.BodyguardAllowed(pawn, Charge));
		Toil toil = ToilMaker.MakeToil("Bodyguard");
		toil.defaultCompleteMode = ToilCompleteMode.Delay;
		toil.defaultDuration = 600;
		toil.initAction = () =>
		{
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null)
			{
				compArsenal.onDutyPost = CompArsenal.For(Charge)?.onDutyPost;
			}
		};
		toil.tickIntervalAction = delegate
		{
			Pawn charge = Charge;
			if (charge != null && charge.Spawned)
			{
				IntVec3 intVec = Formations.SlotFor(pawn, charge);
				if (intVec.IsValid && !(pawn.Position == intVec) && (!pawn.pather.Moving || !(pawn.pather.Destination.Cell == intVec)) && !(pawn.Position.DistanceTo(intVec) <= 1.6f) && pawn.CanReach(intVec, PathEndMode.OnCell, Danger.Deadly))
				{
					pawn.pather.StartPath(intVec, PathEndMode.OnCell);
				}
			}
		};
		toil.AddFinishAction(() =>
		{
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null)
			{
				compArsenal.onDutyPost = null;
			}
		});
		yield return toil;
	}
}
