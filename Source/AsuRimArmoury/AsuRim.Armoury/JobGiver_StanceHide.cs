using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobGiver_StanceHide : JobGiver_StanceBase
{
	private const float ThreatRadius = 45f;

	private const float SearchRadius = 30f;

	private const int MaxSightTests = 220;

	private const float MeleeStrikeRadius = 6f;

	private const int SafetyExpiryTicks = 600;

	private const int StrikeWaitTicks = 60;

	private const int SearchThrottleTicks = 20;

	public override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.Map == null)
		{
			return null;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return null;
		}
		Thing thing = RefreshTarget(pawn, compArsenal);
		if (thing == null)
		{
			return null;
		}
		if (ShouldStrike(pawn))
		{
			Job job = base.TryGiveJob(pawn);
			if (job != null)
			{
				return job;
			}
			return StrikeWait(pawn);
		}
		IntVec3 intVec = FindTrulyUnseenCell(pawn, thing, compArsenal);
		if (!intVec.IsValid)
		{
			return null;
		}
		Job job2 = JobMaker.MakeJob(ASA_PostJobDefOf.ASA_Ambush, intVec, thing);
		job2.expiryInterval = 600;
		job2.checkOverrideOnExpire = true;
		return job2;
	}

	private static Job StrikeWait(Pawn pawn)
	{
		if (!pawn.Drafted)
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.Wait_Combat, pawn.Position);
		job.expiryInterval = 60;
		job.checkOverrideOnExpire = true;
		return job;
	}

	public static bool ShouldStrike(Pawn pawn)
	{
		Map map = pawn?.Map;
		if (map == null)
		{
			return false;
		}
		Verb verb = pawn.TryGetAttackVerb(null, !pawn.IsColonist && !pawn.IsColonySubhuman);
		bool flag = verb != null && !verb.verbProps.IsMeleeAttack;
		float num = (flag ? verb.EffectiveRange : 6f);
		float num2 = num * num;
		float num3 = 2025f;
		List<Pawn> list = StanceTargeting.HostilePawns(map, pawn.Faction);
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn2 = list[i];
			if (pawn2.Position.Fogged(map))
			{
				continue;
			}
			float num4 = pawn.Position.DistanceToSquared(pawn2.Position);
			if (!(num4 > num3))
			{
				if (num4 <= num2 && StanceMovement.TargetAllowed(pawn, pawn2) && (flag ? verb.CanHitTarget(pawn2) : GenSight.LineOfSight(pawn.Position, pawn2.Position, map, skipFirstCell: true)))
				{
					return true;
				}
				if (GenSight.LineOfSight(pawn2.Position, pawn.Position, map, skipFirstCell: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static Thing RefreshTarget(Pawn pawn, CompArsenal comp)
	{
		Thing thing = comp.ambushTarget;
		if (thing != null && (!thing.Spawned || thing.Destroyed || thing.Map != pawn.Map || !(thing is Pawn { Downed: false } pawn2) || !pawn2.HostileTo(pawn) || !((float)pawn.Position.DistanceToSquared(thing.Position) <= 2025f)))
		{
			thing = null;
		}
		if (thing == null)
		{
			thing = NearestThreat(pawn);
		}
		comp.ambushTarget = thing;
		return thing;
	}

	private static Thing NearestThreat(Pawn pawn)
	{
		Map map = pawn.Map;
		Thing result = null;
		float num = 2025f;
		List<Pawn> list = StanceTargeting.HostilePawns(map, pawn.Faction);
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn2 = list[i];
			if (!pawn2.Position.Fogged(map))
			{
				float num2 = pawn.Position.DistanceToSquared(pawn2.Position);
				if (!(num2 > num))
				{
					num = num2;
					result = pawn2;
				}
			}
		}
		return result;
	}

	private static IntVec3 FindTrulyUnseenCell(Pawn pawn, Thing target, CompArsenal comp)
	{
		Map map = pawn.Map;
		IntVec3 position = target.Position;
		if (pawn.Position.InBounds(map) && !GenSight.LineOfSight(position, pawn.Position, map, skipFirstCell: true))
		{
			return pawn.Position;
		}
		int ticksGame = Find.TickManager.TicksGame;
		if (ticksGame < comp.nextAmbushSearchTick)
		{
			return IntVec3.Invalid;
		}
		comp.nextAmbushSearchTick = ticksGame + 20;
		int num = 0;
		foreach (IntVec3 item in GenRadial.RadialCellsAround(pawn.Position, 30f, useCenter: false))
		{
			if (num >= 220)
			{
				break;
			}
			if (item.InBounds(map) && item.Standable(map) && StanceMovement.CellAllowed(pawn, item))
			{
				num++;
				if (!GenSight.LineOfSight(position, item, map, skipFirstCell: true) && pawn.CanReach(item, PathEndMode.OnCell, Danger.Deadly))
				{
					return item;
				}
			}
		}
		return IntVec3.Invalid;
	}
}
