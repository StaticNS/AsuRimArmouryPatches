using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobDriver_DropoffLoadoutItem : JobDriver
{
	private Thing Target => job.GetTarget(TargetIndex.A).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
		Toil toil = ToilMaker.MakeToil("DropoffLoadoutItem");
		toil.initAction = () =>
		{
			Thing thing = Target;
			ThingOwner<Thing> inventory = pawn.inventory?.innerContainer;
			if (thing == null || inventory == null || !inventory.Contains(thing)) {
				return;
			}
			int count = job.count > 0 ? System.Math.Min(job.count, thing.stackCount) : thing.stackCount;
			Thing drop = count < thing.stackCount ? thing.SplitOff(count) : thing;
			if (drop == thing) {
				inventory.Remove(thing);
			}
			if (!GenPlace.TryPlaceThing(drop, job.targetB.Cell, pawn.Map, ThingPlaceMode.Direct)
				&& !GenPlace.TryPlaceThing(drop, job.targetB.Cell, pawn.Map, ThingPlaceMode.Near)) {
				if (!inventory.TryAdd(drop)) {
					Log.Error($"Unable to drop off {drop} for {pawn} or return it to their inventory.");
				}
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return toil;
	}
}
