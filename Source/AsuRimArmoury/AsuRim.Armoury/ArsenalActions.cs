using AsuRimArmoury;
using RimWorld;
using Verse;
using Verse.Sound;

namespace AsuRim.Armoury;

public static class ArsenalActions
{
	public static bool StowOffHand(Pawn pawn, bool drop = false)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		ThingWithComps thingWithComps = compArsenal?.offHand;
		if (thingWithComps == null)
		{
			return true;
		}
		if (drop && PreciousWeapons.ShouldKeep(thingWithComps))
		{
			drop = false;
		}
		if (drop)
		{
			ThingWithComps thingWithComps2 = compArsenal.TakeOffHand();
			if (!GenPlace.TryPlaceThing(thingWithComps2, pawn.Position, pawn.Map, ThingPlaceMode.Near))
			{
				compArsenal.SetOffHand(thingWithComps2);
				return false;
			}
			thingWithComps2.SetForbidden(value: false, warnOnFail: false);
			KitRecovery.Decline(compArsenal, thingWithComps2);
			return true;
		}
		ThingWithComps thingWithComps3 = compArsenal.TakeOffHand();
		if (!pawn.inventory.innerContainer.TryAddOrTransfer(thingWithComps3))
		{
			compArsenal.SetOffHand(thingWithComps3);
			return false;
		}
		Remember(pawn, thingWithComps3);
		return true;
	}

	public static bool StowMainHand(Pawn pawn, bool drop = false)
	{
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		if (thingWithComps == null)
		{
			return true;
		}
		if (drop && PreciousWeapons.ShouldKeep(thingWithComps))
		{
			drop = false;
		}
		if (drop)
		{
			KitRecovery.deliberate++;
			try
			{
				ThingWithComps resultingEq;
				return pawn.equipment.TryDropEquipment(thingWithComps, out resultingEq, pawn.Position, forbid: false);
			}
			finally
			{
				KitRecovery.deliberate--;
			}
		}
		if (!pawn.inventory.innerContainer.TryAddOrTransfer(thingWithComps))
		{
			return false;
		}
		Remember(pawn, thingWithComps);
		return true;
	}

	public static bool EquipMain(Pawn pawn, ThingWithComps weapon, bool dropCurrent = false, bool manual = false)
	{
		if (weapon == null || pawn?.equipment == null)
		{
			return false;
		}
		if (Arsenal.MainHand(pawn) == weapon)
		{
			return true;
		}
		if (manual ? (!ArsenalRules.CanWield(pawn, weapon)) : (!ArsenalRules.CanAutoWield(pawn, weapon)))
		{
			return false;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null && compArsenal.offHand == weapon)
		{
			compArsenal.TakeOffHand();
			if (!StowMainHand(pawn, dropCurrent))
			{
				compArsenal.SetOffHand(weapon);
				return false;
			}
		}
		else
		{
			if (!StowOffHand(pawn))
			{
				return false;
			}
			if (!StowMainHand(pawn, dropCurrent))
			{
				return false;
			}
		}
		if (weapon.stackCount > 1)
		{
			weapon = weapon.SplitOff(1) as ThingWithComps;
		}
		weapon.holdingOwner?.Remove(weapon);
		pawn.equipment.AddEquipment(weapon);
		if (manual && compArsenal != null)
		{
			compArsenal.downMainThing = null;
		}
		PlayEquipSound(pawn, weapon);
		Remember(pawn, weapon);
		return true;
	}

	public static bool EquipOff(Pawn pawn, ThingWithComps weapon, bool manual = false)
	{
		if (weapon == null || pawn?.equipment == null)
		{
			return false;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return false;
		}
		if (compArsenal.offHand == weapon)
		{
			return true;
		}
		if (!manual && ArsenalRules.WouldBond(pawn, weapon))
		{
			return false;
		}
		if (!ArsenalRules.CanDualWield(pawn, Arsenal.MainHand(pawn), weapon))
		{
			return false;
		}
		if (!StowOffHand(pawn))
		{
			return false;
		}
		if (weapon.stackCount > 1)
		{
			weapon = weapon.SplitOff(1) as ThingWithComps;
		}
		weapon.holdingOwner?.Remove(weapon);
		if (!compArsenal.SetOffHand(weapon))
		{
			return false;
		}
		PlayEquipSound(pawn, weapon);
		Remember(pawn, weapon);
		return true;
	}

	public static bool ReleaseOffHand(Pawn pawn, bool forbid = false)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal?.offHand == null)
		{
			return true;
		}
		ThingWithComps thingWithComps = compArsenal.TakeOffHand();
		Map mapHeld = pawn.MapHeld;
		if (mapHeld != null && GenPlace.TryPlaceThing(thingWithComps, pawn.PositionHeld, mapHeld, ThingPlaceMode.Near))
		{
			thingWithComps.SetForbidden(forbid, warnOnFail: false);
			return true;
		}
		if (pawn.inventory != null && pawn.inventory.innerContainer.TryAdd(thingWithComps))
		{
			return true;
		}
		compArsenal.SetOffHand(thingWithComps);
		return false;
	}

	public static void CheckOffHand(Pawn pawn, CompArsenal comp)
	{
		ThingWithComps thingWithComps = comp?.offHand;
		if (thingWithComps == null || pawn == null || pawn.Dead)
		{
			return;
		}
		if (!Arsenal.CanCarryWeapons(pawn))
		{
			ReleaseOffHand(pawn);
			return;
		}
		ThingWithComps thingWithComps2 = Arsenal.MainHand(pawn);
		if (thingWithComps2 == null)
		{
			if (pawn.Downed && pawn.MapHeld != null)
			{
				if (ReleaseOffHand(pawn, forbid: true) && thingWithComps.Spawned)
				{
					comp.downOffThing = thingWithComps;
				}
			}
			else
			{
				PromoteOffHand(pawn, comp, thingWithComps);
			}
		}
		else if (!ArsenalRules.CanHoldPair(pawn, thingWithComps2, thingWithComps) && !StowOffHand(pawn))
		{
			ReleaseOffHand(pawn);
		}
	}

	private static void PromoteOffHand(Pawn pawn, CompArsenal comp, ThingWithComps off)
	{
		if (ArsenalRules.CanWield(pawn, off))
		{
			comp.TakeOffHand();
			pawn.equipment.AddEquipment(off);
			if (pawn.equipment.Primary == off || off.holdingOwner != null)
			{
				return;
			}
			comp.SetOffHand(off);
		}
		if (!StowOffHand(pawn))
		{
			ReleaseOffHand(pawn);
		}
	}

	public static bool GoUnarmed(Pawn pawn)
	{
		if (pawn?.equipment == null)
		{
			return false;
		}
		if (!StowOffHand(pawn))
		{
			return false;
		}
		return StowMainHand(pawn);
	}

	public static void Remember(Pawn pawn, Thing weapon)
	{
		if (!ArmourySettings.sidearmSystemEnabled)
		{
			return;
		}
		CompArsenal comp = CompArsenal.For(pawn);
		if (comp == null || weapon == null || !ArsenalRules.IsWieldable(weapon))
		{
			return;
		}
		if (comp.policy != null)
		{
			comp.stowed.RemoveAll((LoadoutSlot s) => s == null || IsCoveredByPolicy(comp.policy, s.def));
			if (IsCoveredByPolicy(comp.policy, weapon.def))
			{
				return;
			}
			int num = 0;
			for (int num2 = 0; num2 < comp.stowed.Count; num2++)
			{
				if (comp.stowed[num2].def == weapon.def)
				{
					num++;
				}
			}
			if (num < Arsenal.CarriedCount(pawn, weapon.def))
			{
				comp.stowed.Add(new LoadoutSlot(weapon));
			}
			return;
		}
		int num3 = 0;
		for (int num4 = 0; num4 < comp.stowed.Count; num4++)
		{
			if (comp.stowed[num4].def == weapon.def)
			{
				num3++;
			}
		}
		if (num3 >= Arsenal.CarriedCount(pawn, weapon.def))
		{
			return;
		}
		comp.stowed.Add(new LoadoutSlot(weapon));
		if (comp.stowed.Count >= 2 && pawn.IsColonist && KitRecovery.quiet == 0)
		{
			WeaponPolicyDatabase current = WeaponPolicyDatabase.Current;
			if (current != null)
			{
				comp.policy = current.MakeFrom(pawn);
				comp.stowed.Clear();
			}
		}
	}

	public static void RememberQuietly(Pawn pawn, Thing weapon)
	{
		KitRecovery.quiet++;
		try
		{
			Remember(pawn, weapon);
		}
		finally
		{
			KitRecovery.quiet--;
		}
	}

	internal static bool IsCoveredByPolicy(WeaponPolicy policy, ThingDef def)
	{
		if (policy == null)
		{
			return false;
		}
		if (policy.mainDef == def || policy.offDef == def)
		{
			return true;
		}
		for (int i = 0; i < policy.sidearms.Count; i++)
		{
			if (policy.sidearms[i]?.def == def)
			{
				return true;
			}
		}
		return false;
	}

	public static void Forget(Pawn pawn, Thing weapon)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null || weapon == null)
		{
			return;
		}
		KitRecovery.Decline(compArsenal, weapon);
		for (int i = 0; i < compArsenal.stowed.Count; i++)
		{
			if (compArsenal.stowed[i].Matches(weapon))
			{
				compArsenal.stowed.RemoveAt(i);
				break;
			}
		}
	}

	private static void PlayEquipSound(Pawn pawn, Thing weapon)
	{
		if (!ArmourySettings.muteWeaponSwapSound && weapon.def.soundInteract != null && pawn.Map != null)
		{
			weapon.def.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
		}
	}
}
