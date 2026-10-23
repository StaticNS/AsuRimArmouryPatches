using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public static class PolicyApplier
{
	public static bool Apply(Pawn pawn, WeaponPolicy policy, bool applyStance = true)
	{
		if (pawn == null || policy == null)
		{
			return false;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return false;
		}
		if (applyStance && StanceRules.Allows(pawn, policy.stance, out var _))
		{
			StanceGizmo.SetStance(pawn, compArsenal, policy.stance);
		}
		if (compArsenal.forcedUnarmed)
		{
			return true;
		}
		if (policy.mainDef == null)
		{
			return true;
		}
		if (policy.offDef != null)
		{
			ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
			ThingWithComps thingWithComps2 = Arsenal.OffHand(pawn);
			if (thingWithComps != null && thingWithComps2 != null && thingWithComps.def == policy.mainDef && thingWithComps.Stuff == policy.mainStuff && thingWithComps2.def == policy.offDef && thingWithComps2.Stuff == policy.offStuff && LoadoutCoverage.Meets(thingWithComps, policy.mainMinQuality) && LoadoutCoverage.Meets(thingWithComps2, policy.offMinQuality))
			{
				return true;
			}
			ThingWithComps thingWithComps3 = FindCarried(pawn, policy.mainDef, policy.mainStuff, null, policy.mainMinQuality);
			if (thingWithComps3 == null)
			{
				return false;
			}
			ThingWithComps thingWithComps4 = FindCarried(pawn, policy.offDef, policy.offStuff, thingWithComps3, policy.offMinQuality);
			if (thingWithComps4 == null)
			{
				return false;
			}
			if (!ArsenalActions.EquipMain(pawn, thingWithComps3))
			{
				return false;
			}
			ArsenalActions.EquipOff(pawn, thingWithComps4);
			return true;
		}
		ThingWithComps thingWithComps5 = FindCarried(pawn, policy.mainDef, policy.mainStuff, null, policy.mainMinQuality);
		if (thingWithComps5 == null)
		{
			return false;
		}
		if (!ArsenalActions.EquipMain(pawn, thingWithComps5))
		{
			return false;
		}
		ArsenalActions.StowOffHand(pawn);
		return true;
	}

	internal static ThingWithComps FindCarried(Pawn pawn, ThingDef def, ThingDef stuff, ThingWithComps exclude = null, QualityCategory minQuality = QualityCategory.Awful)
	{
		ThingWithComps thingWithComps = null;
		ThingWithComps thingWithComps2 = pawn.equipment?.Primary;
		ThingWithComps thingWithComps3 = Arsenal.OffHand(pawn);
		ThingWithComps[] array = new ThingWithComps[2] { thingWithComps2, thingWithComps3 };
		foreach (ThingWithComps thingWithComps4 in array)
		{
			if (thingWithComps4 != null && thingWithComps4 != exclude && thingWithComps4.def == def && LoadoutCoverage.Meets(thingWithComps4, minQuality) && ArsenalRules.CanAutoWield(pawn, thingWithComps4))
			{
				if (thingWithComps4.Stuff == stuff)
				{
					return thingWithComps4;
				}
				if (thingWithComps == null)
				{
					thingWithComps = thingWithComps4;
				}
			}
		}
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner != null)
		{
			for (int j = 0; j < thingOwner.Count; j++)
			{
				if (thingOwner[j] is ThingWithComps thingWithComps5 && thingWithComps5 != exclude && thingWithComps5.def == def && LoadoutCoverage.Meets(thingWithComps5, minQuality) && ArsenalRules.CanAutoWield(pawn, thingWithComps5))
				{
					if (thingWithComps5.Stuff == stuff)
					{
						return thingWithComps5;
					}
					if (thingWithComps == null)
					{
						thingWithComps = thingWithComps5;
					}
				}
			}
		}
		return thingWithComps;
	}
}
