using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobDriver_ReArm : JobDriver
{
	private enum Step
	{
		None,
		Fetch,
		Equip,
		Dropoff
	}

	private Step step;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		Toil selectNext = ToilMaker.MakeToil("SelectNextReArmTask");
		selectNext.initAction = () =>
		{
			Job task = LoadoutGearJobs.NextJob(pawn);
			if (task == null || task.targetA.Thing == null)
			{
				EndJobWith(JobCondition.Succeeded);
				return;
			}

			step = task.def == AsuRimJobDefOf.ASA_DropoffLoadoutItem
				? Step.Dropoff
				: task.def == JobDefOf.Equip ? Step.Equip : Step.Fetch;
			job.targetA = task.targetA;
			job.targetB = step == Step.Dropoff
				? task.targetB
				: new LocalTargetInfo(task.targetA.Thing.Spawned ? task.targetA.Thing.Position : pawn.Position);
			job.count = task.count;

			if (step != Step.Dropoff && task.targetA.Thing.Spawned
				&& !pawn.Reserve(task.targetA, job, 1, task.count, null, false))
			{
				EndJobWith(JobCondition.Incompletable);
			}
		};
		selectNext.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return selectNext;

		yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch);

		Toil perform = ToilMaker.MakeToil("PerformReArmTask");
		perform.initAction = PerformStep;
		perform.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return perform;
		yield return Toils_Jump.Jump(selectNext);
	}

	private void PerformStep()
	{
		Thing target = job.targetA.Thing;
		if (target == null || target.Destroyed)
		{
			EndJobWith(JobCondition.Incompletable);
			return;
		}

		switch (step)
		{
			case Step.Fetch:
				TakeToInventory(target);
				break;
			case Step.Equip:
				Equip(target as ThingWithComps);
				break;
			case Step.Dropoff:
				Dropoff(target);
				break;
			default:
				EndJobWith(JobCondition.Incompletable);
				break;
		}
	}

	private void TakeToInventory(Thing target)
	{
		ThingOwner<Thing> inventory = pawn.inventory?.innerContainer;
		if (inventory == null || !target.Spawned)
		{
			EndJobWith(JobCondition.Incompletable);
			return;
		}

		int count = job.count > 0 ? System.Math.Min(job.count, target.stackCount) : target.stackCount;
		Thing taken = count < target.stackCount ? target.SplitOff(count) : target;
		if (taken == target)
		{
			target.DeSpawn();
		}
		if (!inventory.TryAdd(taken)
			&& !GenPlace.TryPlaceThing(taken, pawn.Position, pawn.Map, ThingPlaceMode.Near))
		{
			Log.Error($"Unable to return {taken} to the map after {pawn} failed to pick it up.");
			EndJobWith(JobCondition.Incompletable);
		}
	}

	private void Equip(ThingWithComps weapon)
	{
		if (weapon == null)
		{
			EndJobWith(JobCondition.Incompletable);
			return;
		}

		if (weapon.Spawned)
		{
			weapon.DeSpawn();
		}
		if (!ArsenalActions.EquipMain(pawn, weapon))
		{
			if (!(pawn.inventory?.innerContainer.TryAdd(weapon) ?? false)
				&& !GenPlace.TryPlaceThing(weapon, pawn.Position, pawn.Map, ThingPlaceMode.Near))
			{
				Log.Error($"Unable to return {weapon} to the map after {pawn} failed to equip it.");
			}
			EndJobWith(JobCondition.Incompletable);
		}
	}

	private void Dropoff(Thing target)
	{
		ThingOwner<Thing> inventory = pawn.inventory?.innerContainer;
		if (inventory == null || !inventory.Contains(target))
		{
			EndJobWith(JobCondition.Incompletable);
			return;
		}

		int count = job.count > 0 ? System.Math.Min(job.count, target.stackCount) : target.stackCount;
		Thing dropped = count < target.stackCount ? target.SplitOff(count) : target;
		if (dropped == target)
		{
			inventory.Remove(target);
		}
		if (!GenPlace.TryPlaceThing(dropped, job.targetB.Cell, pawn.Map, ThingPlaceMode.Direct)
			&& !GenPlace.TryPlaceThing(dropped, job.targetB.Cell, pawn.Map, ThingPlaceMode.Near)
			&& !inventory.TryAdd(dropped))
		{
			Log.Error($"Unable to drop off {dropped} for {pawn} or return it to their inventory.");
			EndJobWith(JobCondition.Incompletable);
		}
	}
}
