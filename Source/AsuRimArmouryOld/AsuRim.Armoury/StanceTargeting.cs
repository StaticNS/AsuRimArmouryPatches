using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class StanceTargeting
{
	private const int ScanThrottleTicks = 15;

	private static readonly List<Pawn> hostileBuffer = new List<Pawn>();

	private static readonly HashSet<Pawn> hostileSeen = new HashSet<Pawn>();

	public static Thing ThrottledScan(Pawn pawn, Func<Thing> scan)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return scan();
		}
		int ticksGame = Find.TickManager.TicksGame;
		if (ticksGame < compArsenal.nextStanceScanTick)
		{
			Thing cachedStanceScanTarget = compArsenal.cachedStanceScanTarget;
			if (cachedStanceScanTarget == null || !cachedStanceScanTarget.Spawned || cachedStanceScanTarget.Destroyed)
			{
				return null;
			}
			return cachedStanceScanTarget;
		}
		compArsenal.nextStanceScanTick = ticksGame + 15;
		return compArsenal.cachedStanceScanTarget = scan();
	}

	public static List<Pawn> HostilePawns(Map map, Faction faction)
	{
		hostileBuffer.Clear();
		if (map == null)
		{
			return hostileBuffer;
		}
		if (faction == null)
		{
			IReadOnlyList<Pawn> aggroCandidates = map.mapPawns.AllPawnsSpawned;
			for (int i = 0; i < aggroCandidates.Count; i++)
			{
				Pawn pawn = aggroCandidates[i];
				if (pawn != null && !pawn.Dead && !pawn.Downed && pawn.HostileTo(faction))
				{
					hostileBuffer.Add(pawn);
				}
			}
			return hostileBuffer;
		}
		hostileSeen.Clear();
		foreach (IAttackTarget item in map.attackTargetsCache.TargetsHostileToFaction(faction))
		{
			if (item.Thing is Pawn other)
			{
				AddHostile(other, faction);
			}
		}
		IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
		for (int i = 0; i < allPawnsSpawned.Count; i++)
		{
			Pawn pawn = allPawnsSpawned[i];
			if (pawn != null && pawn.InAggroMentalState)
			{
				AddHostile(pawn, faction);
			}
		}
		hostileSeen.Clear();
		return hostileBuffer;
	}

	private static void AddHostile(Pawn other, Faction faction)
	{
		if (other != null && !other.Dead && !other.Downed && other.Spawned && other.HostileTo(faction) && hostileSeen.Add(other))
		{
			hostileBuffer.Add(other);
		}
	}

	public static Thing WeakestHostile(Pawn pawn, float radius)
	{
		Map map = pawn?.Map;
		if (map == null)
		{
			return null;
		}
		float num = radius * radius;
		Thing result = null;
		float num2 = float.MaxValue;
		List<Pawn> list = HostilePawns(map, pawn.Faction);
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn2 = list[i];
			if (!pawn2.Position.Fogged(map) && !((float)pawn.Position.DistanceToSquared(pawn2.Position) > num) && StanceMovement.TargetAllowed(pawn, pawn2))
			{
				float num3 = pawn2.health.summaryHealth.SummaryHealthPercent * pawn2.HealthScale * 100f;
				num3 *= 1f + pawn2.GetStatValue(StatDefOf.ArmorRating_Sharp);
				num3 += pawn.Position.DistanceTo(pawn2.Position) * 0.35f;
				if (num3 < num2)
				{
					num2 = num3;
					result = pawn2;
				}
			}
		}
		return result;
	}

	public static bool PassiveAnimal(Pawn other)
	{
		if (!other.NonHumanlikeOrWildMan() || other.IsMutant || other.IsAttacking())
		{
			return false;
		}
		if (other.InAggroMentalState)
		{
			return false;
		}
		return other.Faction == null;
	}

	public static bool IsMeleePawn(Pawn pawn)
	{
		Pawn_EquipmentTracker equipment = pawn.equipment;
		if (equipment == null)
		{
			return true;
		}
		return equipment.Primary?.def.IsRangedWeapon != true;
	}

	public static Thing NearestHostileForCovering(Pawn pawn, float radius)
	{
		Map map = pawn?.Map;
		if (map == null)
		{
			return null;
		}
		Thing best = null;
		float bestDist = float.MaxValue;
		Squad squad = SquadManager.Current?.SquadOf(pawn);
		if (squad != null)
		{
			List<Pawn> members = squad.Members;
			List<Pawn> list = null;
			for (int i = 0; i < members.Count; i++)
			{
				if (CanCover(members[i], pawn, map))
				{
					if (list == null)
					{
						list = HostilePawns(map, pawn.Faction);
					}
					ScanForNearestHostile(members[i], list, map, radius, ref best, ref bestDist);
				}
			}
			return best;
		}
		Pawn pawn2 = NearestMeleeAlly(pawn, map);
		if (pawn2 != null)
		{
			ScanForNearestHostile(pawn2, HostilePawns(map, pawn.Faction), map, radius, ref best, ref bestDist);
		}
		return best;
	}

	private static bool CanCover(Pawn melee, Pawn pawn, Map map)
	{
		if (melee != null && melee != pawn && !melee.Dead && !melee.Downed && melee.Spawned && melee.Map == map)
		{
			return IsMeleePawn(melee);
		}
		return false;
	}

	private static void ScanForNearestHostile(Pawn melee, List<Pawn> hostiles, Map map, float radius, ref Thing best, ref float bestDist)
	{
		for (int i = 0; i < hostiles.Count; i++)
		{
			Pawn pawn = hostiles[i];
			if (!pawn.Position.Fogged(map))
			{
				float num = melee.Position.DistanceTo(pawn.Position);
				if (!(num > radius) && !(num >= bestDist))
				{
					bestDist = num;
					best = pawn;
				}
			}
		}
	}

	private static Pawn NearestMeleeAlly(Pawn pawn, Map map)
	{
		Pawn result = null;
		float num = float.MaxValue;
		IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
		for (int i = 0; i < allPawnsSpawned.Count; i++)
		{
			Pawn pawn2 = allPawnsSpawned[i];
			if (CanCover(pawn2, pawn, map) && pawn2.Faction == pawn.Faction)
			{
				float num2 = pawn.Position.DistanceTo(pawn2.Position);
				if (!(num2 >= num))
				{
					num = num2;
					result = pawn2;
				}
			}
		}
		return result;
	}
}
