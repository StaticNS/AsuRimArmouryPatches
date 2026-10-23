using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ArmouryTactics.DualWield;

internal class JobDriver_EquipOffHand : JobDriver
{
	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		Pawn p = pawn;
		LocalTargetInfo targetA = base.job.targetA;
		Job job = base.job;
		return p.Reserve(targetA, job, 1, -1, null, errorOnFailed);
	}

	public override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		this.FailOnBurningImmobile(TargetIndex.A);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
		yield return new Toil
		{
			initAction = () =>
			{
				ThingWithComps thingWithComps = (ThingWithComps)job.targetA.Thing;
				ThingWithComps thingWithComps2;
				if (thingWithComps.def.stackLimit > 1 && thingWithComps.stackCount > 1)
				{
					thingWithComps2 = (ThingWithComps)thingWithComps.SplitOff(1);
				}
				else
				{
					thingWithComps2 = thingWithComps;
					thingWithComps2.DeSpawn();
				}
				thingWithComps2.SetWeaponAsOffHanded(set: true);
				pawn.equipment.MakeRoomFor(thingWithComps2);
				pawn.SetOffHander(thingWithComps2);
				if (thingWithComps.def.soundInteract != null)
				{
					thingWithComps.def.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
				}
			},
			defaultCompleteMode = ToilCompleteMode.Instant
		};
	}
}
