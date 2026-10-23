using System.Collections.Generic;
using ArmouryTactics;
using ArmouryTactics.Compatability;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class StanceKiting
{
	private const float TriggerRange = 8f;

	private const float CloseRange = 4.5f;

	private const float SearchRadius = 6.9f;

	private const float MinGain = 2f;

	private const float TravelWeight = 0.35f;

	private const int ExpiryTicks = 30;

	private const int MaxReachTests = 4;

	private static readonly List<Pawn> threats = new List<Pawn>();

	private static readonly List<KeyValuePair<IntVec3, float>> cells = new List<KeyValuePair<IntVec3, float>>();

	public static Job TryKite(Pawn pawn)
	{
		if (!Wants(pawn))
		{
			return null;
		}
		Map map = pawn.Map;
		Pawn pawn2 = CollectThreats(pawn, map, out var nearestDist);
		if (pawn2 == null)
		{
			return null;
		}
		try
		{
			IntVec3 intVec = FindCell(pawn, map, pawn2, nearestDist);
			if (!intVec.IsValid)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Goto, intVec);
			job.expiryInterval = 30;
			job.checkOverrideOnExpire = true;
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null)
			{
				compArsenal.combatProtectionUntil = Find.TickManager.TicksGame + 300;
			}
			return job;
		}
		finally
		{
			threats.Clear();
			cells.Clear();
		}
	}

	private static bool Wants(Pawn pawn)
	{
		if (pawn?.Map == null || !pawn.Spawned || pawn.Downed || pawn.InMentalState)
		{
			return false;
		}
		if (!ArmourySettings.walkFireEnabled || !ModSettings_ArmouryTactics.runAndGunEnabled)
		{
			return false;
		}
		if (CombatExtendedCompat.Active)
		{
			return false;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null || !compArsenal.walkFire || !compArsenal.UnderCombatOrders)
		{
			return false;
		}
		if (StanceMovement.StanceOf(pawn) != CombatStance.Engage)
		{
			return false;
		}
		if (!StanceMovement.HoldsGun(pawn) || pawn.carryTracker?.CarriedThing != null)
		{
			return false;
		}
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return false;
		}
		Verb verb = pawn.equipment.PrimaryEq?.PrimaryVerb;
		if (verb != null && !verb.verbProps.IsMeleeAttack)
		{
			return verb.EffectiveRange > 8f;
		}
		return false;
	}

	private static Pawn CollectThreats(Pawn pawn, Map map, out float nearestDist)
	{
		threats.Clear();
		nearestDist = float.MaxValue;
		Pawn result = null;
		IntVec3 position = pawn.Position;
		List<Pawn> list = StanceTargeting.HostilePawns(map, pawn.Faction);
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn2 = list[i];
			if (!StanceTargeting.IsMeleePawn(pawn2) || StanceTargeting.PassiveAnimal(pawn2) || pawn2.Position.Fogged(map))
			{
				continue;
			}
			float num = pawn2.Position.DistanceTo(position);
			if (!(num > 8f) && (!(num > 4.5f) || Closing(pawn2, pawn)))
			{
				threats.Add(pawn2);
				if (num < nearestDist)
				{
					nearestDist = num;
					result = pawn2;
				}
			}
		}
		return result;
	}

	private static bool Closing(Pawn other, Pawn pawn)
	{
		if (other.mindState?.enemyTarget == pawn)
		{
			return true;
		}
		Pawn_PathFollower pather = other.pather;
		if (pather == null || !pather.Moving)
		{
			return false;
		}
		IntVec3 cell = pather.Destination.Cell;
		if (!cell.IsValid)
		{
			return false;
		}
		return cell.DistanceToSquared(pawn.Position) < other.Position.DistanceToSquared(pawn.Position);
	}

	private static IntVec3 FindCell(Pawn pawn, Map map, Pawn nearest, float nearestDist)
	{
		cells.Clear();
		IntVec3 position = pawn.Position;
		int num = GenRadial.NumCellsInRadius(6.9f);
		for (int i = 1; i < num; i++)
		{
			IntVec3 intVec = position + GenRadial.RadialPattern[i];
			if (intVec.InBounds(map) && intVec.Standable(map))
			{
				float num2 = Clearance(intVec);
				if (!(num2 < nearestDist + 2f) && StanceMovement.CellAllowed(pawn, intVec) && intVec.GetFirstPawn(map) == null && !PawnUtility.KnownDangerAt(intVec, map, pawn) && GenSight.LineOfSight(intVec, nearest.Position, map, skipFirstCell: true))
				{
					cells.Add(new KeyValuePair<IntVec3, float>(intVec, num2 - position.DistanceTo(intVec) * 0.35f));
				}
			}
		}
		if (cells.Count == 0)
		{
			return IntVec3.Invalid;
		}
		cells.Sort((KeyValuePair<IntVec3, float> a, KeyValuePair<IntVec3, float> b) => b.Value.CompareTo(a.Value));
		int num3 = Mathf.Min(4, cells.Count);
		for (int num4 = 0; num4 < num3; num4++)
		{
			IntVec3 key = cells[num4].Key;
			if (pawn.CanReach(key, PathEndMode.OnCell, Danger.Some))
			{
				return key;
			}
		}
		return IntVec3.Invalid;
	}

	private static float Clearance(IntVec3 c)
	{
		float num = float.MaxValue;
		for (int i = 0; i < threats.Count; i++)
		{
			float num2 = threats[i].Position.DistanceTo(c);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}
}
