using System.Runtime.CompilerServices;
using AsuRim.Armoury;
using Verse;

namespace ArmouryTactics;

internal static class DualWieldExtensions
{
	public static bool IsOffHandedWeapon(this Thing thing)
	{
		return Arsenal.IsOffHand(thing);
	}

	public static void SetWeaponAsOffHanded(this Thing thing, bool set)
	{
		if (thing is ThingWithComps thingWithComps)
		{
			Pawn pawn = (thingWithComps.ParentHolder as CompArsenal)?.Pawn;
			if (!set)
			{
				pawn?.SetOffHander(thingWithComps, removing: true);
			}
		}
	}

	public static bool IsTwoHanded(this Def def)
	{
		return ModSettings_ArmouryTactics.twoHandersCache.Contains(def.shortHash);
	}

	public static bool HasTwoHander(this Pawn pawn)
	{
		return pawn.equipment?.Primary?.def.IsTwoHanded() == true;
	}

	public static bool CanBeOffHand(this Def def)
	{
		return ModSettings_ArmouryTactics.offHandersCache.Contains(def.shortHash);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool HasOffHand(this Pawn pawn)
	{
		ThingWithComps thing;
		return pawn.GetOffHander(out thing);
	}

	public static bool GetOffHander(this Pawn pawn, out ThingWithComps thing)
	{
		thing = Arsenal.OffHand(pawn);
		return thing != null;
	}

	public static ThingWithComps MainHand(this Pawn pawn)
	{
		return Arsenal.MainHand(pawn);
	}

	public static void SetOffHander(this Pawn pawn, ThingWithComps thing, bool removing = false)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null)
		{
			if (removing)
			{
				compArsenal.TakeOffHand();
				return;
			}
			compArsenal.SetOffHand(thing);
			ArsenalActions.Remember(pawn, thing);
		}
	}

	public static Stance GetOffHandStance(this Pawn pawn)
	{
		return pawn.GetOffHandStanceTracker()?.curStance;
	}

	public static Pawn_StanceTracker GetOffHandStanceTracker(this Pawn pawn)
	{
		return AsuRim.Armoury.DualWield.OffHandStances(pawn);
	}
}
