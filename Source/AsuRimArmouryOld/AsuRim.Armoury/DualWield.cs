using System.Collections.Generic;
using ArmouryTactics;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class DualWield
{
	private static readonly List<Verb> usableVerbs = new List<Verb>();

	private static readonly List<VerbEntry> verbEntries = new List<VerbEntry>();

	public const float MeleeRange = 1.74f;

	public static Pawn_StanceTracker OffHandStances(Pawn pawn)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return null;
		}
		if (compArsenal.offHandStances == null)
		{
			compArsenal.offHandStances = new Pawn_StanceTracker(pawn);
		}
		return compArsenal.offHandStances;
	}

	public static Stance CurrentOffHandStance(Pawn pawn)
	{
		return CompArsenal.For(pawn)?.offHandStances?.curStance;
	}

	public static bool OffHandBusy(Pawn pawn)
	{
		if (CurrentOffHandStance(pawn) is Stance_Busy stance_Busy)
		{
			return stance_Busy.StanceBusy;
		}
		return false;
	}

	internal static bool WalkFiring(Pawn pawn)
	{
		if (ModSettings_ArmouryTactics.runAndGunEnabled && ArmourySettings.walkFireEnabled)
		{
			Pawn_PathFollower pather = pawn.pather;
			if (pather != null && pather.Moving)
			{
				return CompArsenal.For(pawn)?.walkFire ?? false;
			}
		}
		return false;
	}

	public static void Tick(Pawn pawn, CompArsenal comp)
	{
		ThingWithComps offHand = comp.offHand;
		if (offHand != null)
		{
			offHand.DoTick();
			if (!offHand.Destroyed && offHand.def.tickerType != TickerType.Normal)
			{
				offHand.GetComp<CompEquippable>()?.verbTracker.VerbsTick();
			}
			offHand = comp.offHand;
		}
		if (comp.offHandStances != null)
		{
			if (offHand == null)
			{
				comp.offHandStances = null;
			}
			else
			{
				comp.offHandStances.StanceTrackerTick();
			}
		}
	}

	public static Verb OffHandVerb(Pawn pawn)
	{
		return Arsenal.OffHand(pawn)?.GetComp<CompEquippable>()?.PrimaryVerb;
	}

	public static bool TryGetOffHandMeleeVerb(Pawn pawn, Thing target, out Verb verb)
	{
		verb = null;
		ThingWithComps thingWithComps = Arsenal.OffHand(pawn);
		if (thingWithComps == null)
		{
			return false;
		}
		List<Verb> list = thingWithComps.GetComp<CompEquippable>()?.AllVerbs;
		if (list == null)
		{
			return false;
		}
		List<Verb> list2 = usableVerbs;
		list2.Clear();
		float num = 0f;
		int count = list.Count;
		while (count-- > 0)
		{
			Verb verb2 = list[count];
			if (verb2.IsMeleeAttack && verb2.IsStillUsableBy(pawn))
			{
				list2.Add(verb2);
				float num2 = VerbUtility.InitialVerbWeight(verb2, pawn);
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		if (list2.Count == 0)
		{
			return false;
		}
		List<VerbEntry> list3 = verbEntries;
		list3.Clear();
		for (int i = 0; i < list2.Count; i++)
		{
			list3.Add(new VerbEntry(list2[i], pawn, list2, num));
		}
		bool flag = list3.TryRandomElementByWeight((VerbEntry ve) => ve.GetSelectionWeight(target), out var result);
		list3.Clear();
		list2.Clear();
		if (!flag)
		{
			return false;
		}
		verb = result.verb;
		return true;
	}

	public static bool TryStartOffHandMelee(Pawn pawn, Thing target)
	{
		if (!ArmourySettings.dualWieldEnabled)
		{
			return false;
		}
		if (OffHandBusy(pawn) || pawn.InMentalState)
		{
			return false;
		}
		if (!TryGetOffHandMeleeVerb(pawn, target, out var verb))
		{
			return false;
		}
		OffHandFiring.Begin(pawn);
		try
		{
			return verb.TryStartCastOn(target);
		}
		finally
		{
			OffHandFiring.End();
		}
	}

	public static bool OffHandIsEffective(Pawn pawn)
	{
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		ThingWithComps thingWithComps2 = Arsenal.OffHand(pawn);
		if (thingWithComps == null || thingWithComps2 == null)
		{
			return false;
		}
		if (thingWithComps.def.IsMeleeWeapon == thingWithComps2.def.IsMeleeWeapon)
		{
			return false;
		}
		Thing thing = pawn.mindState?.enemyTarget;
		bool flag = thing != null && pawn.Position.DistanceTo(thing.Position) <= 1.74f;
		if (!thingWithComps.def.IsMeleeWeapon)
		{
			return flag;
		}
		return !flag;
	}

	public static Verb EffectiveVerbFor(Pawn pawn, Thing target, Verb fallback)
	{
		if (fallback == null || !ArmouryTracked.OffHand.Contains(pawn) || !ArmourySettings.dualWieldEnabled)
		{
			return fallback;
		}
		if (!fallback.IsMeleeAttack)
		{
			return fallback;
		}
		ThingWithComps thingWithComps = Arsenal.OffHand(pawn);
		if (thingWithComps == null || thingWithComps.def.IsMeleeWeapon)
		{
			return fallback;
		}
		if (pawn.MannedThing() != null)
		{
			return fallback;
		}
		Verb verb = thingWithComps.GetComp<CompEquippable>()?.PrimaryVerb;
		if (verb == null || !verb.Available())
		{
			return fallback;
		}
		if (target != null && pawn.Position.DistanceTo(target.Position) <= 1.74f)
		{
			return fallback;
		}
		return verb;
	}
}
