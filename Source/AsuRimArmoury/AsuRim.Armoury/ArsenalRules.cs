using System.Collections.Generic;
using ArmouryTactics;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public static class ArsenalRules
{
	public static bool IsTwoHanded(ThingDef def)
	{
		if (def == null)
		{
			return false;
		}
		if (TryGetExplicitHands(def, out var twoHanded))
		{
			return twoHanded;
		}
		return ArmourySettings.InferTwoHanded(def);
	}

	public static bool TryGetExplicitHands(ThingDef def, out bool twoHanded)
	{
		twoHanded = false;
		if (def == null)
		{
			return false;
		}
		if (TryGetPlayerOverride(def, out twoHanded))
		{
			return true;
		}
		ArmouryWeaponExtension modExtension = def.GetModExtension<ArmouryWeaponExtension>();
		if (modExtension != null && modExtension.hands != WeaponHands.Unset)
		{
			twoHanded = modExtension.hands == WeaponHands.Two;
			return true;
		}
		return false;
	}

	private static bool TryGetPlayerOverride(ThingDef def, out bool twoHanded)
	{
		twoHanded = false;
		HashSet<ushort> twoHandersCache = ModSettings_ArmouryTactics.twoHandersCache;
		HashSet<ushort> offHandersCache = ModSettings_ArmouryTactics.offHandersCache;
		if (twoHandersCache == null || offHandersCache == null)
		{
			return false;
		}
		ushort shortHash = def.shortHash;
		if (twoHandersCache.Contains(shortHash))
		{
			twoHanded = true;
			return true;
		}
		if (offHandersCache.Contains(shortHash))
		{
			twoHanded = false;
			return true;
		}
		return false;
	}

	public static bool IsWieldable(ThingDef def)
	{
		if (def != null && def.equipmentType == EquipmentType.Primary && def.stackLimit <= 1 && (def.IsRangedWeapon || def.IsMeleeWeapon))
		{
			return def.HasComp<CompEquippable>();
		}
		return false;
	}

	public static bool IsWieldable(Thing thing)
	{
		if (thing is ThingWithComps)
		{
			return IsWieldable(thing.def);
		}
		return false;
	}

	public static bool CanWield(Pawn pawn, Thing thing)
	{
		if (pawn == null || !IsWieldable(thing))
		{
			return false;
		}
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return false;
		}
		if (thing.def.IsRangedWeapon && pawn.WorkTagIsDisabled(WorkTags.Shooting))
		{
			return false;
		}
		if (pawn.equipment == null)
		{
			return false;
		}
		if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(thing, pawn))
		{
			return false;
		}
		return EquipmentUtility.CanEquip(thing, pawn);
	}

	public static bool WouldBond(Pawn pawn, Thing thing)
	{
		CompBiocodable compBiocodable = (thing as ThingWithComps)?.GetComp<CompBiocodable>();
		if (compBiocodable == null || !compBiocodable.Biocodable)
		{
			return false;
		}
		if (pawn != null && compBiocodable.CodedPawn == pawn)
		{
			return false;
		}
		if (compBiocodable is CompBladelinkWeapon)
		{
			return true;
		}
		return (compBiocodable.props as CompProperties_Biocodable)?.biocodeOnEquip ?? false;
	}

	public static bool IsRangedStance(CombatStance stance)
	{
		if (stance != CombatStance.Engage && stance != CombatStance.Suppress && stance != CombatStance.Covering && stance != CombatStance.Immobilise)
		{
			return stance == CombatStance.Hide;
		}
		return true;
	}

	public static bool HoldsGunInRangedStance(Pawn pawn)
	{
		if (pawn == null || pawn.InMentalState || !ArmourySettings.stancesEnabled)
		{
			return false;
		}
		if (pawn.Faction == null || !pawn.Faction.IsPlayer)
		{
			return false;
		}
		ThingWithComps thingWithComps = pawn.equipment?.Primary;
		if (thingWithComps == null || !thingWithComps.def.IsRangedWeapon)
		{
			return false;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null || !compArsenal.UnderCombatOrders)
		{
			return false;
		}
		if (!IsRangedStance(ArmouryStances.Effective(compArsenal)))
		{
			return false;
		}
		ThingWithComps thingWithComps2 = Arsenal.OffHand(pawn);
		if (thingWithComps2 != null && thingWithComps2.def.IsMeleeWeapon)
		{
			return false;
		}
		return (pawn.equipment.PrimaryEq?.PrimaryVerb)?.Available() ?? false;
	}

	public static bool CanAutoWield(Pawn pawn, Thing thing)
	{
		if (CanWield(pawn, thing))
		{
			return !WouldBond(pawn, thing);
		}
		return false;
	}

	public static bool CanBeOffHand(ThingDef def)
	{
		if (def == null)
		{
			return false;
		}
		if (!IsWieldable(def))
		{
			return false;
		}
		if (IsTwoHanded(def))
		{
			return false;
		}
		ArmouryWeaponExtension modExtension = def.GetModExtension<ArmouryWeaponExtension>();
		if (modExtension != null && modExtension.neverOffHand)
		{
			return false;
		}
		return true;
	}

	public static bool CanDualWield(Pawn pawn, ThingWithComps main, ThingWithComps candidate)
	{
		if (!ArmourySettings.dualWieldEnabled)
		{
			return false;
		}
		return CanHoldPair(pawn, main, candidate);
	}

	public static bool CanHoldPair(Pawn pawn, ThingWithComps main, ThingWithComps off)
	{
		if (pawn == null || main == null || off == null)
		{
			return false;
		}
		if (!Arsenal.CanCarryWeapons(pawn))
		{
			return false;
		}
		if (!CanBeOffHand(off.def))
		{
			return false;
		}
		if (IsTwoHanded(main.def))
		{
			return false;
		}
		if (HasMissingHand(pawn))
		{
			return false;
		}
		if (VEFShieldCompat.Active && VEFShieldCompat.OffHandShield(pawn) != null)
		{
			return false;
		}
		return CanWield(pawn, off);
	}

	public static bool HasMissingHand(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			return false;
		}
		List<Hediff_MissingPart> missingPartsCommonAncestors = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
		int count = missingPartsCommonAncestors.Count;
		while (count-- > 0)
		{
			BodyPartDef bodyPartDef = missingPartsCommonAncestors[count].Part?.def;
			if (bodyPartDef == BodyPartDefOf.Hand || bodyPartDef == BodyPartDefOf.Arm)
			{
				return true;
			}
		}
		return false;
	}
}
