using System.Collections.Generic;
using ArmouryTactics.DualWield;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class KitRecovery
{
	internal static int deliberate;

	internal static int quiet;

	public static bool IsDeliberateDrop(bool scoped, bool outOfAction, JobDriver driver)
	{
		if (scoped)
		{
			return true;
		}
		if (outOfAction)
		{
			return false;
		}
		if (!(driver is JobDriver_DropEquipment) && !(driver is JobDriver_Equip) && !(driver is JobDriver_UseOutfitStand))
		{
			return driver is JobDriver_EquipOffHand;
		}
		return true;
	}

	public static bool OutOfAction(Pawn pawn)
	{
		if (pawn == null)
		{
			return false;
		}
		if (pawn.Downed)
		{
			return true;
		}
		PawnCapacitiesHandler pawnCapacitiesHandler = pawn.health?.capacities;
		if (pawnCapacitiesHandler != null)
		{
			return !pawnCapacitiesHandler.CapableOf(PawnCapacityDefOf.Manipulation);
		}
		return false;
	}

	internal static bool Tracks(Pawn pawn)
	{
		if (pawn != null && !pawn.Dead && pawn.Faction != null && pawn.Faction.IsPlayer && pawn.RaceProps != null)
		{
			return pawn.RaceProps.Humanlike;
		}
		return false;
	}

	internal static void NoteMainDropped(Pawn pawn, ThingWithComps weapon)
	{
		if (weapon == null || !Tracks(pawn) || !ArsenalRules.IsWieldable(weapon))
		{
			return;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return;
		}
		bool flag = deliberate > 0;
		if (IsDeliberateDrop(flag, OutOfAction(pawn), pawn.jobs?.curDriver))
		{
			Forget(compArsenal, weapon);
			if (compArsenal.liveMain == weapon)
			{
				compArsenal.liveMain = null;
			}
			if (!flag)
			{
				Job curJob = pawn.CurJob;
				if (curJob == null || !curJob.playerForced)
				{
					return;
				}
			}
			Decline(compArsenal, weapon);
		}
		else
		{
			compArsenal.downMainThing = weapon;
		}
	}

	public static void NoteSidearmDropped(CompArsenal comp, ThingWithComps weapon)
	{
		if (comp != null && weapon != null)
		{
			if (comp.downSidearms == null)
			{
				comp.downSidearms = new List<ThingWithComps>();
			}
			if (!comp.downSidearms.Contains(weapon))
			{
				comp.downSidearms.Add(weapon);
			}
		}
	}

	public static bool IsKit(CompArsenal comp, Thing thing)
	{
		if (comp == null || thing == null)
		{
			return false;
		}
		if (ArsenalActions.IsCoveredByPolicy(comp.policy, thing.def))
		{
			return true;
		}
		for (int i = 0; i < comp.stowed.Count; i++)
		{
			if (comp.stowed[i] != null && comp.stowed[i].Matches(thing))
			{
				return true;
			}
		}
		return false;
	}

	public static void Forget(CompArsenal comp, Thing thing)
	{
		if (comp != null && thing != null)
		{
			if (comp.downMainThing == thing)
			{
				comp.downMainThing = null;
			}
			if (comp.downOffThing == thing)
			{
				comp.downOffThing = null;
			}
			comp.downSidearms?.Remove(thing as ThingWithComps);
		}
	}

	public static void Clear(CompArsenal comp)
	{
		if (comp != null)
		{
			comp.downMainThing = null;
			comp.downOffThing = null;
			comp.downSidearms?.Clear();
		}
	}

	public static bool HasPending(CompArsenal comp)
	{
		if (comp != null)
		{
			if (comp.downMainThing == null && comp.downOffThing == null)
			{
				if (comp.downSidearms != null)
				{
					return comp.downSidearms.Count > 0;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static bool Pending(CompArsenal comp, ThingDef def)
	{
		if (comp == null || def == null)
		{
			return false;
		}
		if (comp.downMainThing != null && !comp.downMainThing.Destroyed && comp.downMainThing.def == def)
		{
			return true;
		}
		if (comp.downOffThing != null && !comp.downOffThing.Destroyed && comp.downOffThing.def == def)
		{
			return true;
		}
		if (comp.downSidearms == null)
		{
			return false;
		}
		for (int i = 0; i < comp.downSidearms.Count; i++)
		{
			ThingWithComps thingWithComps = comp.downSidearms[i];
			if (thingWithComps != null && !thingWithComps.Destroyed && thingWithComps.def == def)
			{
				return true;
			}
		}
		return false;
	}

	public static bool MainPending(Pawn pawn, CompArsenal comp)
	{
		ThingWithComps thingWithComps = comp?.downMainThing;
		if (thingWithComps != null && !thingWithComps.Destroyed && thingWithComps.Spawned && pawn != null)
		{
			return thingWithComps.Map == pawn.MapHeld;
		}
		return false;
	}

	public static void Decline(CompArsenal comp, Thing thing)
	{
		if (comp != null && thing is ThingWithComps thingWithComps)
		{
			Forget(comp, thingWithComps);
			if (comp.declined == null)
			{
				comp.declined = new List<ThingWithComps>();
			}
			if (!comp.declined.Contains(thingWithComps))
			{
				comp.declined.Add(thingWithComps);
			}
		}
	}

	public static void Undecline(CompArsenal comp, Thing thing)
	{
		comp?.declined?.Remove(thing as ThingWithComps);
	}

	public static bool Declined(CompArsenal comp, Thing thing)
	{
		if (comp?.declined != null && comp.declined.Count > 0)
		{
			return comp.declined.Contains(thing as ThingWithComps);
		}
		return false;
	}

	public static void Prune(Pawn pawn, CompArsenal comp)
	{
		if (comp != null)
		{
			if (comp.downMainThing != null && comp.downMainThing.Destroyed)
			{
				comp.downMainThing = null;
			}
			if (comp.downOffThing != null && comp.downOffThing.Destroyed)
			{
				comp.downOffThing = null;
			}
			comp.downSidearms?.RemoveAll((ThingWithComps t) => t?.Destroyed ?? true);
			comp.declined?.RemoveAll((ThingWithComps t) => t == null || t.Destroyed || HeldByPawn(t));
		}
	}

	private static bool HeldByPawn(Thing thing)
	{
		IThingHolder parentHolder = thing.ParentHolder;
		if (!(parentHolder is Pawn_EquipmentTracker) && !(parentHolder is Pawn_InventoryTracker))
		{
			return parentHolder is CompArsenal;
		}
		return true;
	}

	public static bool MayFetch(Pawn pawn)
	{
		if (Tracks(pawn) && pawn.Spawned && !pawn.Downed && !pawn.InMentalState && !pawn.Drafted && !pawn.IsPrisoner && pawn.IsColonist)
		{
			return Arsenal.CanCarryWeapons(pawn);
		}
		return false;
	}

	public static Job NextJob(Pawn pawn, CompArsenal comp)
	{
		if (!HasPending(comp))
		{
			return null;
		}
		if (!MayFetch(pawn))
		{
			if (pawn.IsPrisoner || pawn.Dead)
			{
				Clear(comp);
			}
			return null;
		}
		Job job = FetchJob(pawn, comp, comp.downMainThing, equip: true);
		if (job != null)
		{
			return job;
		}
		if (pawn.mindState?.enemyTarget != null)
		{
			return null;
		}
		job = FetchJob(pawn, comp, comp.downOffThing, equip: true);
		if (job != null)
		{
			return job;
		}
		if (comp.downSidearms == null)
		{
			return null;
		}
		for (int num = comp.downSidearms.Count - 1; num >= 0; num--)
		{
			if (num < comp.downSidearms.Count)
			{
				job = FetchJob(pawn, comp, comp.downSidearms[num], equip: false);
				if (job != null)
				{
					return job;
				}
			}
		}
		return null;
	}

	private static Job FetchJob(Pawn pawn, CompArsenal comp, ThingWithComps thing, bool equip)
	{
		if (thing == null)
		{
			return null;
		}
		if (thing.Destroyed)
		{
			Forget(comp, thing);
			return null;
		}
		if (!thing.Spawned)
		{
			if (!(thing.ParentHolder is Pawn_CarryTracker))
			{
				Forget(comp, thing);
			}
			return null;
		}
		if (thing.Map != pawn.Map)
		{
			return null;
		}
		if (equip ? (!ArsenalRules.CanAutoWield(pawn, thing)) : (!ArsenalRules.CanWield(pawn, thing)))
		{
			Forget(comp, thing);
			return null;
		}
		if (thing.IsBurning())
		{
			return null;
		}
		if (!equip && MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, thing, 1))
		{
			return null;
		}
		if (!pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
		{
			return null;
		}
		Job job = JobMaker.MakeJob(AsuRimJobDefOf.ASA_RetrieveOffHand, thing);
		job.ignoreForbidden = true;
		return job;
	}
}
