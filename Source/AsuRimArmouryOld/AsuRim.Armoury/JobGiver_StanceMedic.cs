using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobGiver_StanceMedic : JobGiver_StanceBase
{
	private const float RescueSearchRadius = 60f;

	private const float DangerRadius = 8f;

	public JobGiver_StanceMedic()
	{
		targetAcquireRadius = 45f;
		targetKeepRadius = 55f;
		chaseTarget = false;
	}

	protected override Job TryGiveJob(Pawn pawn)
	{
		return TryRescueJob(pawn) ?? base.TryGiveJob(pawn);
	}

	private Job TryRescueJob(Pawn pawn)
	{
		if (pawn.Map == null)
		{
			return null;
		}
		Job curJob = pawn.CurJob;
		if (curJob != null && curJob.def == JobDefOf.Rescue && curJob.targetA.Thing is Pawn { Downed: not false } pawn2 && (pawn2.Spawned || pawn.carryTracker?.CarriedThing == pawn2))
		{
			return curJob;
		}
		if (!(StanceTargeting.ThrottledScan(pawn, () => FindRescueCandidate(pawn)) is Pawn { Downed: not false } pawn3) || !HealthAIUtility.CanRescueNow(pawn, pawn3, forced: true))
		{
			return null;
		}
		Building_Bed building_Bed = RestUtility.FindPatientBedFor(pawn3);
		if (building_Bed == null || !pawn3.CanReserve(building_Bed))
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.Rescue, pawn3, building_Bed);
		job.count = 1;
		return job;
	}

	private static Thing FindRescueCandidate(Pawn pawn)
	{
		Pawn result = null;
		float num = float.MaxValue;
		List<Pawn> spawnedDownedPawns = pawn.Map.mapPawns.SpawnedDownedPawns;
		for (int i = 0; i < spawnedDownedPawns.Count; i++)
		{
			Pawn pawn2 = spawnedDownedPawns[i];
			if (pawn2 != pawn && pawn2.Faction == pawn.Faction && HealthAIUtility.CanRescueNow(pawn, pawn2, forced: true) && !IsTooDangerous(pawn2) && StanceMovement.CellAllowed(pawn, pawn2.Position))
			{
				float num2 = pawn.Position.DistanceToSquared(pawn2.Position);
				if (!(num2 > 3600f) && !(num2 >= num))
				{
					num = num2;
					result = pawn2;
				}
			}
		}
		return result;
	}

	private static bool IsTooDangerous(Pawn downed)
	{
		Map map = downed.Map;
		if (map == null)
		{
			return true;
		}
		List<Pawn> list = StanceTargeting.HostilePawns(map, downed.Faction);
		for (int i = 0; i < list.Count; i++)
		{
			if (downed.Position.DistanceTo(list[i].Position) <= 8f)
			{
				return true;
			}
		}
		return false;
	}
}
