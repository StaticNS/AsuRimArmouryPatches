using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobDriver_ManPost : JobDriver
{
	private const int RecheckInterval = 1250;

	private bool facingApplied;

	private CompGuardPost Post => job.targetA.Thing?.TryGetComp<CompGuardPost>();

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		this.FailOn(() => GuardStandDown.Active || Post == null || !Post.IsAssigned(pawn));
		yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
		Toil toil = ToilMaker.MakeToil("ManPost");
		toil.defaultCompleteMode = ToilCompleteMode.Delay;
		toil.defaultDuration = 1250;
		toil.handlingFacing = true;
		toil.initAction = () =>
		{
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null)
			{
				compArsenal.onDutyPost = Post;
			}
			MechPostChargingUtility.TryStart(pawn, Post);
		};
		toil.tickIntervalAction = (int delta) =>
		{
			CompGuardPost post = Post;
			if (post != null)
			{
				CompArsenal compArsenal = CompArsenal.For(pawn);
				if (compArsenal != null)
				{
					compArsenal.onDutyPost = post;
				}
				if (!facingApplied)
				{
					facingApplied = true;
					pawn.Rotation = post.facing.ToRot();
				}
				PostedCombat.Check(pawn, delta);
			}
		};
		toil.AddFinishAction(() =>
		{
			facingApplied = false;
			MechPostChargingUtility.TryStop(pawn, Post);
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null && compArsenal.onDutyPost == Post)
			{
				compArsenal.onDutyPost = null;
			}
		});
		yield return toil;
	}
}
