using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AsuRim.Armoury;

public class JobDriver_RetrieveOffHand : JobDriver
{
	private ThingWithComps Target => (ThingWithComps)job.GetTarget(TargetIndex.A).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
	}

	public override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
		Toil toil = ToilMaker.MakeToil("RetrieveOffHand");
		toil.initAction = () =>
		{
			ThingWithComps target = Target;
			if (target == null || target.Destroyed)
			{
				return;
			}
			CompArsenal compArsenal = CompArsenal.For(pawn);
			bool flag = compArsenal != null && compArsenal.downMainThing == target;
			bool flag2 = compArsenal?.downSidearms != null && compArsenal.downSidearms.Contains(target);
			KitRecovery.Forget(compArsenal, target);
			KitRecovery.Undecline(compArsenal, target);
			target.SetForbidden(value: false, warnOnFail: false);
			ThingWithComps thingWithComps = target;
			if (target.def.stackLimit > 1 && target.stackCount > 1)
			{
				thingWithComps = (ThingWithComps)target.SplitOff(1);
			}
			else
			{
				thingWithComps.DeSpawn();
			}
			KitRecovery.quiet++;
			try
			{
				if (!(flag ? ArsenalActions.EquipMain(pawn, thingWithComps) : (!flag2 && ArsenalActions.EquipOff(pawn, thingWithComps, job.playerForced))))
				{
					if (pawn.inventory.innerContainer.TryAdd(thingWithComps))
					{
						ArsenalActions.Remember(pawn, thingWithComps);
					}
					else
					{
						GenPlace.TryPlaceThing(thingWithComps, pawn.Position, pawn.Map, ThingPlaceMode.Near);
					}
					thingWithComps.def.soundInteract?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
				}
			}
			finally
			{
				KitRecovery.quiet--;
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return toil;
	}
}
