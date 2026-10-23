using RimWorld;
using Verse;

namespace AsuRim.Armoury;

internal static class KitRestore
{
	internal static void Check(Pawn pawn, CompArsenal comp)
	{
		KitRecovery.Prune(pawn, comp);
		if (pawn.IsPrisoner)
		{
			KitRecovery.Clear(comp);
		}
		if (!Arsenal.CanCarrySidearms(pawn))
		{
			return;
		}
		if (pawn.Downed || pawn.InMentalState || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
		{
			if (!comp.wasOutOfAction && comp.downMain == null && comp.downOff == null)
			{
				Capture(pawn, comp);
			}
			comp.wasOutOfAction = true;
			return;
		}
		if (comp.downMain == null && comp.downOff == null)
		{
			NoteLiveKit(pawn, comp);
		}
		if (comp.wasOutOfAction)
		{
			comp.wasOutOfAction = false;
			if (comp.policy != null)
			{
				if (!KitRecovery.MainPending(pawn, comp))
				{
					PolicyApplier.Apply(pawn, comp.policy, applyStance: false);
				}
				comp.downMain = null;
				comp.downOff = null;
				return;
			}
		}
		Restore(pawn, comp);
	}

	private static void NoteLiveKit(Pawn pawn, CompArsenal comp)
	{
		ThingWithComps liveOff = Arsenal.OffHand(pawn);
		ThingWithComps liveMain = Arsenal.MainHand(pawn);
		comp.liveOff = liveOff;
		comp.liveMain = liveMain;
	}

	private static void Capture(Pawn pawn, CompArsenal comp)
	{
		ThingWithComps thingWithComps = Held(pawn, comp.liveOff) ?? Arsenal.OffHand(pawn);
		ThingWithComps thingWithComps2 = Held(pawn, comp.liveMain) ?? Arsenal.MainHand(pawn);
		if (thingWithComps2 == thingWithComps)
		{
			thingWithComps = null;
		}
		comp.downMain = ((thingWithComps2 != null) ? new LoadoutSlot(thingWithComps2) : null);
		comp.downOff = ((thingWithComps != null) ? new LoadoutSlot(thingWithComps) : null);
		comp.liveMain = null;
		comp.liveOff = null;
		if (thingWithComps2 != null)
		{
			ArsenalActions.RememberQuietly(pawn, thingWithComps2);
		}
		if (thingWithComps != null)
		{
			ArsenalActions.RememberQuietly(pawn, thingWithComps);
		}
	}

	private static ThingWithComps Held(Pawn pawn, ThingWithComps weapon)
	{
		if (weapon == null || weapon.Destroyed)
		{
			return null;
		}
		IThingHolder parentHolder = weapon.ParentHolder;
		if ((parentHolder as Pawn_EquipmentTracker)?.pawn == pawn)
		{
			return weapon;
		}
		if ((parentHolder as Pawn_InventoryTracker)?.pawn == pawn)
		{
			return weapon;
		}
		if ((parentHolder as CompArsenal)?.Pawn == pawn)
		{
			return weapon;
		}
		if (weapon.Map == null)
		{
			return null;
		}
		return weapon;
	}

	private static void Restore(Pawn pawn, CompArsenal comp)
	{
		if ((comp.downMain == null && comp.downOff == null) || pawn.mindState?.enemyTarget != null || !RangeDirector.SafeToSwap(pawn))
		{
			return;
		}
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		if (thingWithComps == null && comp.downMain != null)
		{
			if (KitRecovery.MainPending(pawn, comp))
			{
				return;
			}
			thingWithComps = FindCarried(pawn, comp.downMain) ?? BestCarried(pawn);
			if (thingWithComps != null)
			{
				ArsenalActions.EquipMain(pawn, thingWithComps);
			}
			thingWithComps = Arsenal.MainHand(pawn);
		}
		if (thingWithComps == null)
		{
			return;
		}
		if (comp.downOff == null)
		{
			comp.downMain = null;
			return;
		}
		if (Arsenal.OffHand(pawn) != null)
		{
			comp.downMain = null;
			comp.downOff = null;
			return;
		}
		ThingWithComps thingWithComps2 = FindCarried(pawn, comp.downOff);
		if (thingWithComps2 != null && ArsenalActions.EquipOff(pawn, thingWithComps2))
		{
			comp.downMain = null;
			comp.downOff = null;
		}
	}

	private static ThingWithComps FindCarried(Pawn pawn, LoadoutSlot slot)
	{
		if (slot?.def == null)
		{
			return null;
		}
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner == null)
		{
			return null;
		}
		for (int i = 0; i < thingOwner.Count; i++)
		{
			if (thingOwner[i] is ThingWithComps thingWithComps && slot.Matches(thingWithComps) && ArsenalRules.CanAutoWield(pawn, thingWithComps))
			{
				return thingWithComps;
			}
		}
		return null;
	}

	private static ThingWithComps BestCarried(Pawn pawn)
	{
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner == null)
		{
			return null;
		}
		ThingWithComps result = null;
		float num = 0f;
		for (int i = 0; i < thingOwner.Count; i++)
		{
			if (thingOwner[i] is ThingWithComps thingWithComps && ArsenalRules.CanAutoWield(pawn, thingWithComps))
			{
				float num2 = WeaponScorer.Score(pawn, thingWithComps, 12f);
				if (num2 > num)
				{
					num = num2;
					result = thingWithComps;
				}
			}
		}
		return result;
	}
}
