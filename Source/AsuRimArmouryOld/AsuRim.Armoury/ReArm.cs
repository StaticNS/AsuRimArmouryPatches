using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AsuRim.Armoury;

internal static class ReArm {

	internal static void Order(Pawn pawn) {
		List<Pawn> pawns = Commanded.With(pawn);
		for (int i = 0; i < pawns.Count; i++) {
			Pawn commandedPawn = pawns[i];
			if (CompArsenal.For(commandedPawn) != null && commandedPawn.jobs != null) {
				Job job = JobMaker.MakeJob(AsuRimJobDefOf.ASA_ReArm);
				job.playerForced = true;
				commandedPawn.jobs.TryTakeOrderedJob(job);
			}
		}
		SoundDefOf.Click.PlayOneShotOnCamera();
	}

	internal static int CountInInventory(Pawn pawn, ThingDef def) {
		ThingOwner<Thing> inventory = pawn.inventory?.innerContainer;
		if (inventory == null) {
			return 0;
		}
		int count = 0;
		for (int i = 0; i < inventory.Count; i++) {
			if (inventory[i].def == def) {
				count += inventory[i].stackCount;
			}
		}
		return count;
	}

	internal static int DesiredStockCount(WeaponPolicy policy, ThingDef def) {
		long count = 0;
		count += DesiredStockCount(policy.ammo, def);
		count += DesiredStockCount(policy.grenades, def);
		count += DesiredStockCount(policy.meals, def);
		count += DesiredStockCount(policy.medicine, def);
		return (int)System.Math.Min(count, int.MaxValue);
	}

	private static int DesiredStockCount(List<LoadoutSlot> slots, ThingDef def) {
		if (slots == null) {
			return 0;
		}
		long count = 0;
		for (int i = 0; i < slots.Count; i++) {
			LoadoutSlot slot = slots[i];
			if (slot?.def == def && slot.count > 0) {
				count += slot.count;
			}
		}
		return (int)System.Math.Min(count, int.MaxValue);
	}

	internal static Job FindDropoffJob(Pawn pawn, CompArsenal comp) {
		ThingOwner<Thing> inventory = pawn.inventory?.innerContainer;
		if (inventory == null) {
			return null;
		}
		WeaponPolicy policy = comp.policy;
		for (int i = 0; i < inventory.Count; i++) {
			Thing thing = inventory[i];
			int excess;
			if (ArsenalRules.IsWieldable(thing)) {
				if (LoadoutCoverage.Covers(pawn, thing)) {
					continue;
				}
				excess = thing.stackCount;
			} else {
				if (policy == null) {
					continue;
				}
				int desired = DesiredStockCount(policy, thing.def);
				if (desired == 0) {
					continue;
				}
				excess = CountInInventory(pawn, thing.def) - desired;
				if (excess <= 0) {
					continue;
				}
				excess = System.Math.Min(excess, thing.stackCount);
			}

			IntVec3 cell = pawn.Position;
			if (!StoreUtility.TryFindBestBetterStoreCellFor(thing, pawn, pawn.Map,
				StoragePriority.Unstored, pawn.Faction, out cell)) {
				cell = pawn.Position;
			}
			Job job = JobMaker.MakeJob(AsuRimJobDefOf.ASA_DropoffLoadoutItem, thing, cell);
			job.count = excess;
			job.checkOverrideOnExpire = true;
			return job;
		}
		return null;
	}

}