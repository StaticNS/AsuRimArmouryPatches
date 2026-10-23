using System.Collections.Generic;
using ArmouryTactics.Compatability;
using Verse;

namespace AsuRim.Armoury;

public static class Arsenal
{
	public static bool SidearmsStoodDownForCE
	{
		get
		{
			if (ArmourySettings.standDownForCE)
			{
				return CombatExtendedCompat.Active;
			}
			return false;
		}
	}

	public static ThingWithComps OffHand(Pawn pawn)
	{
		return CompArsenal.For(pawn)?.offHand;
	}

	public static bool IsOffHand(Thing thing)
	{
		if (thing != null)
		{
			return thing.ParentHolder is CompArsenal;
		}
		return false;
	}

	public static ThingWithComps MainHand(Pawn pawn)
	{
		return pawn?.equipment?.Primary;
	}

	public static bool IsDualWielding(Pawn pawn)
	{
		if (OffHand(pawn) != null)
		{
			return MainHand(pawn) != null;
		}
		return false;
	}

	public static int CarriedCount(Pawn pawn, ThingDef def)
	{
		if (pawn == null || def == null)
		{
			return 0;
		}
		int num = 0;
		ThingWithComps thingWithComps = pawn.equipment?.Primary;
		if (thingWithComps != null && thingWithComps.def == def)
		{
			num += thingWithComps.stackCount;
		}
		ThingWithComps thingWithComps2 = OffHand(pawn);
		if (thingWithComps2 != null && thingWithComps2.def == def)
		{
			num += thingWithComps2.stackCount;
		}
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner != null)
		{
			for (int i = 0; i < thingOwner.Count; i++)
			{
				if (thingOwner[i].def == def)
				{
					num += thingOwner[i].stackCount;
				}
			}
		}
		return num;
	}

	public static void CollectAvailable(Pawn pawn, List<ThingWithComps> into, bool includeHeld = true)
	{
		into.Clear();
		if (pawn == null)
		{
			return;
		}
		if (includeHeld)
		{
			ThingWithComps thingWithComps = pawn.equipment?.Primary;
			if (thingWithComps != null)
			{
				into.Add(thingWithComps);
			}
			ThingWithComps thingWithComps2 = OffHand(pawn);
			if (thingWithComps2 != null)
			{
				into.Add(thingWithComps2);
			}
		}
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner == null)
		{
			return;
		}
		for (int i = 0; i < thingOwner.Count; i++)
		{
			if (thingOwner[i] is ThingWithComps thingWithComps3 && ArsenalRules.IsWieldable(thingWithComps3))
			{
				into.Add(thingWithComps3);
			}
		}
	}

	public static bool CanCarryWeapons(Pawn pawn)
	{
		if (pawn != null && pawn.equipment != null && pawn.inventory != null && !pawn.Dead && pawn.RaceProps != null && pawn.RaceProps.Humanlike)
		{
			return !pawn.IsSubhuman;
		}
		return false;
	}

	public static bool CanCarrySidearms(Pawn pawn)
	{
		if (SidearmsStoodDownForCE)
		{
			return false;
		}
		return CanCarryWeapons(pawn);
	}
}
