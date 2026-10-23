using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

internal static class LoadoutGearJobs
{
	private const float SearchRadius = 60f;

	internal static Job NextJob(Pawn pawn)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null || !ArmourySettings.sidearmSystemEnabled || !Arsenal.CanCarrySidearms(pawn) || pawn.Map == null)
		{
			return null;
		}
		if (compArsenal.policy == null && compArsenal.stowed.Count == 0)
		{
			return null;
		}
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return null;
		}
		WeaponPolicy policy = compArsenal.policy;
		if (policy != null && pawn.mindState?.enemyTarget == null)
		{
			PolicyApplier.Apply(pawn, policy, applyStance: false);
		}
		if (policy != null)
		{
			Job job = MainHandJob(pawn, policy);
			if (job != null)
			{
				return job;
			}
			job = FetchJob(pawn, policy.offDef, policy.offStuff);
			if (job != null)
			{
				return job;
			}
			for (int i = 0; i < policy.sidearms.Count; i++)
			{
				LoadoutSlot loadoutSlot = policy.sidearms[i];
				if (loadoutSlot?.def != null)
				{
					job = FetchJob(pawn, loadoutSlot.def, loadoutSlot.stuff);
					if (job != null)
					{
						return job;
					}
				}
			}
			job = StockJob(pawn, policy.ammo, policy);
			if (job != null)
			{
				return job;
			}
			job = StockJob(pawn, policy.grenades, policy);
			if (job != null)
			{
				return job;
			}
			job = StockJob(pawn, policy.meals, policy);
			if (job != null)
			{
				return job;
			}
			job = StockJob(pawn, policy.medicine, policy);
			if (job != null)
			{
				return job;
			}
			return ReArm.FindDropoffJob(pawn, compArsenal);
		}
		for (int num = compArsenal.stowed.Count - 1; num >= 0; num--)
		{
			LoadoutSlot loadoutSlot2 = compArsenal.stowed[num];
			if (loadoutSlot2?.def != null)
			{
				if (Arsenal.CarriedCount(pawn, loadoutSlot2.def) <= 0)
				{
					if (!KitRecovery.Pending(compArsenal, loadoutSlot2.def))
					{
						compArsenal.stowed.RemoveAt(num);
					}
				}
				else
				{
					Job job2 = FetchJob(pawn, loadoutSlot2.def, loadoutSlot2.stuff);
					if (job2 != null)
					{
						return job2;
					}
				}
			}
		}
		return ReArm.FindDropoffJob(pawn, compArsenal);
	}

	private static Job StockJob(Pawn pawn, List<LoadoutSlot> slots, WeaponPolicy policy)
	{
		if (slots == null)
		{
			return null;
		}
		for (int i = 0; i < slots.Count; i++)
		{
			LoadoutSlot loadoutSlot = slots[i];
			if (loadoutSlot?.def == null || loadoutSlot.count <= 0)
			{
				continue;
			}
			int num = CountInInventory(pawn, loadoutSlot.def);
			int desired = ReArm.DesiredStockCount(policy, loadoutSlot.def);
			if (num < desired)
			{
				Thing thing = Nearest(pawn, loadoutSlot.def, equippable: false, weapon: false);
				if (thing != null)
				{
					Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, thing);
					job.count = Mathf.Min(desired - num, thing.stackCount);
					job.checkOverrideOnExpire = true;
					return job;
				}
			}
		}
		return null;
	}

	private static int CountInInventory(Pawn pawn, ThingDef def)
	{
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < thingOwner.Count; i++)
		{
			if (thingOwner[i].def == def)
			{
				num += thingOwner[i].stackCount;
			}
		}
		return num;
	}

	private static Job MainHandJob(Pawn pawn, WeaponPolicy policy)
	{
		if (policy.mainDef == null)
		{
			return null;
		}
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		if (thingWithComps?.def == policy.mainDef && LoadoutCoverage.Meets(thingWithComps, policy.mainMinQuality))
		{
			return null;
		}
		if (KitRecovery.MainPending(pawn, CompArsenal.For(pawn)))
		{
			return null;
		}
		if (PolicyApplier.FindCarried(pawn, policy.mainDef, policy.mainStuff, null, policy.mainMinQuality) != null)
		{
			return null;
		}
		Thing thing = Nearest(pawn, policy.mainDef, equippable: true, weapon: true, policy.mainMinQuality);
		if (thing == null)
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.Equip, thing);
		job.checkOverrideOnExpire = true;
		return job;
	}

	private static Job FetchJob(Pawn pawn, ThingDef def, ThingDef stuff)
	{
		if (def == null)
		{
			return null;
		}
		if (!LoadoutCoverage.Unmet(pawn, def, out var min))
		{
			return null;
		}
		Thing thing = Nearest(pawn, def, equippable: false, weapon: true, min);
		if (thing == null)
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, thing);
		job.count = 1;
		job.checkOverrideOnExpire = true;
		return job;
	}

	private static Thing Nearest(Pawn pawn, ThingDef def, bool equippable, bool weapon, QualityCategory minQuality = QualityCategory.Awful)
	{
		if (weapon && !ArsenalRules.IsWieldable(def))
		{
			return null;
		}
		CompArsenal comp = CompArsenal.For(pawn);
		return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(def), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 60f, (Thing t) => !t.IsForbidden(pawn) && pawn.CanReserve(t) && LoadoutCoverage.Meets(t, minQuality) && (!weapon || (ArsenalRules.CanAutoWield(pawn, t) && !KitRecovery.Declined(comp, t))) && (equippable || MassUtility.CountToPickUpUntilOverEncumbered(pawn, t) > 0));
	}
}
