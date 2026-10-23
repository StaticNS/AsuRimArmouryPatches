using System;
using System.Collections;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

internal static class SurveillanceShiftConversion
{
	private const int SpotCapacity = 4;

	private static readonly List<CompGuardPost> routeBuffer = new List<CompGuardPost>();

	internal static void Run(Map map)
	{
		if (!GuardStandDown.Active || map == null || !SurveillanceShiftBridge.ConversionReady)
		{
			return;
		}
		GuardRegistry guardRegistry = GuardRegistry.For(map);
		if (guardRegistry == null)
		{
			return;
		}
		int num = 0;
		int spots = 0;
		int num2 = 0;
		int num3 = 0;
		int guards = 0;
		int num4 = 0;
		int num5 = 0;
		List<CompGuardPost> list = new List<CompGuardPost>(guardRegistry.Posts);
		for (int i = 0; i < list.Count; i++)
		{
			CompGuardPost compGuardPost = list[i];
			if (compGuardPost != null && compGuardPost.Kind == PostKind.Watch && Live(compGuardPost, map))
			{
				if (TryConvertWatch(compGuardPost, map, ref spots, ref guards))
				{
					num++;
				}
				else
				{
					num5++;
				}
			}
		}
		foreach (PostGroup value in Enum.GetValues(typeof(PostGroup)))
		{
			guardRegistry.Route(value, routeBuffer);
			List<CompGuardPost> list2 = new List<CompGuardPost>();
			for (int j = 0; j < routeBuffer.Count; j++)
			{
				if (Live(routeBuffer[j], map))
				{
					list2.Add(routeBuffer[j]);
				}
			}
			routeBuffer.Clear();
			if (list2.Count != 0)
			{
				if (TryConvertRoute(list2, map, ref guards))
				{
					num2++;
					num3 += list2.Count;
				}
				else
				{
					num5++;
				}
			}
		}
		num4 = ConvertBodyguards(map);
		if (num + num2 + num4 + num5 != 0)
		{
			Log.Message("[Rules of Engagement] Surveillance Shift is active. On " + map?.ToString() + ": " + num + " watchpoints became " + spots + " guard spots, " + num3 + " patrol markers became " + num2 + " patrol routes, " + guards + " guard assignments and " + num4 + " bodyguard links carried over" + ((num5 > 0) ? (", " + num5 + " posts could not be converted and were left in place") : "") + ".");
		}
	}

	private static bool Live(CompGuardPost post, Map map)
	{
		if (post?.parent != null && post.parent.Spawned && post.parent.Map == map)
		{
			return !post.parent.Destroyed;
		}
		return false;
	}

	private static bool TryConvertWatch(CompGuardPost post, Map map, ref int spots, ref int guards)
	{
		List<Pawn> list = new List<Pawn>(post.Manning);
		int num = Math.Max(1, (list.Count + 4 - 1) / 4);
		Rot4 rot = post.facing.ToRot();
		object value = SurveillanceShiftBridge.StanceFor(post.stance);
		List<Thing> list2 = new List<Thing>();
		try
		{
			IntVec3 position = post.parent.Position;
			for (int i = 0; i < num; i++)
			{
				IntVec3 loc = ((i == 0) ? position : FreeCellNear(position, map, list2));
				if (!loc.IsValid)
				{
					throw new InvalidOperationException("no free cell");
				}
				Thing thing = ThingMaker.MakeThing(SurveillanceShiftBridge.guardPostDef);
				thing.SetFactionDirect(Faction.OfPlayer);
				SurveillanceShiftBridge.postStance.SetValue(thing, value);
				GenSpawn.Spawn(thing, loc, map, rot);
				if (!thing.Spawned || thing.TryGetComp<CompAssignableToPawn>() == null)
				{
					throw new InvalidOperationException("spot did not spawn");
				}
				list2.Add(thing);
			}
		}
		catch (Exception ex)
		{
			Undo(list2);
			Log.Warning("[Rules of Engagement] Could not convert " + post.parent?.ToString() + " at " + post.parent.Position.ToString() + ": " + ex.Message);
			return false;
		}
		for (int j = 0; j < list.Count; j++)
		{
			Pawn pawn = list[j];
			if (pawn != null && !pawn.Dead)
			{
				CompAssignableToPawn compAssignableToPawn = list2[j / 4].TryGetComp<CompAssignableToPawn>();
				try
				{
					compAssignableToPawn.TryAssignPawn(pawn);
					guards++;
				}
				catch (Exception ex2)
				{
					Log.Warning("[Rules of Engagement] Could not assign " + pawn?.ToString() + " to a guard spot: " + ex2.Message);
				}
			}
		}
		spots += list2.Count;
		post.parent.Destroy();
		return true;
	}

	private static bool TryConvertRoute(List<CompGuardPost> nodes, Map map, ref int guards)
	{
		MapComponent mapComponent = SurveillanceShiftBridge.GuardingFor(map);
		if (mapComponent == null)
		{
			return false;
		}
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < nodes.Count; i++)
		{
			List<Pawn> manning = nodes[i].Manning;
			for (int j = 0; j < manning.Count; j++)
			{
				if (manning[j] != null && !manning[j].Dead && !list.Contains(manning[j]))
				{
					list.Add(manning[j]);
				}
			}
		}
		List<Thing> list2 = new List<Thing>();
		object obj = null;
		try
		{
			obj = SurveillanceShiftBridge.createRoute.Invoke(mapComponent, new object[1]);
			int num = (int)SurveillanceShiftBridge.routeId.GetValue(obj);
			SurveillanceShiftBridge.routeLoopMode.SetValue(obj, SurveillanceShiftBridge.LoopModeFor(nodes[0].loops));
			SurveillanceShiftBridge.routeStance.SetValue(obj, SurveillanceShiftBridge.StanceFor(nodes[0].stance));
			for (int k = 0; k < nodes.Count; k++)
			{
				Thing thing = ThingMaker.MakeThing(SurveillanceShiftBridge.waypointDef);
				thing.SetFactionDirect(Faction.OfPlayer);
				SurveillanceShiftBridge.waypointRouteId.SetValue(thing, num);
				SurveillanceShiftBridge.waypointDwell.SetValue(thing, Math.Max(0, nodes[k].dwellTicks));
				GenSpawn.Spawn(thing, nodes[k].parent.Position, map, Rot4.North);
				if (!thing.Spawned)
				{
					throw new InvalidOperationException("waypoint did not spawn");
				}
				list2.Add(thing);
			}
		}
		catch (Exception ex)
		{
			Undo(list2);
			if (obj != null)
			{
				(SurveillanceShiftBridge.routes.GetValue(mapComponent) as IList)?.Remove(obj);
			}
			Exception baseException = ex.GetBaseException();
			Log.Warning("[Rules of Engagement] Could not convert the " + nodes[0].group.ToString() + " patrol route: " + baseException.Message);
			return false;
		}
		for (int l = 0; l < list.Count; l++)
		{
			try
			{
				SurveillanceShiftBridge.assignRoute.Invoke(null, new object[2]
				{
					list[l],
					obj
				});
				guards++;
			}
			catch (Exception ex2)
			{
				Log.Warning("[Rules of Engagement] Could not assign " + list[l]?.ToString() + " to a patrol route: " + ex2.GetBaseException().Message);
			}
		}
		for (int m = 0; m < nodes.Count; m++)
		{
			if (!nodes[m].parent.Destroyed)
			{
				nodes[m].parent.Destroy();
			}
		}
		return true;
	}

	private static int ConvertBodyguards(Map map)
	{
		int num = 0;
		List<Pawn> list = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn = list[i];
			if (pawn.Faction == null || !pawn.Faction.IsPlayer)
			{
				continue;
			}
			CompArsenal compArsenal = CompArsenal.For(pawn);
			Pawn pawn2 = compArsenal?.guarding;
			if (pawn2 == null || GuardStandDown.IsSquadEscort(pawn, pawn2))
			{
				continue;
			}
			try
			{
				if (SurveillanceShiftBridge.SetProtectee(pawn, pawn2))
				{
					compArsenal.guarding = null;
					num++;
				}
			}
			catch (Exception ex)
			{
				Log.Warning("[Rules of Engagement] Could not carry over " + pawn?.ToString() + " guarding " + pawn2?.ToString() + ": " + ex.GetBaseException().Message);
			}
		}
		return num;
	}

	private static IntVec3 FreeCellNear(IntVec3 origin, Map map, List<Thing> placed)
	{
		foreach (IntVec3 item in GenRadial.RadialCellsAround(origin, 3.9f, useCenter: false))
		{
			if (item.InBounds(map) && item.Standable(map) && item.GetFirstThing(map, SurveillanceShiftBridge.guardPostDef) == null && !(item.GetFirstBuilding(map) is Building_GuardMarker))
			{
				return item;
			}
		}
		return IntVec3.Invalid;
	}

	private static void Undo(List<Thing> placed)
	{
		for (int num = placed.Count - 1; num >= 0; num--)
		{
			try
			{
				if (!placed[num].Destroyed)
				{
					placed[num].Destroy();
				}
			}
			catch (Exception)
			{
			}
		}
		placed.Clear();
	}
}
