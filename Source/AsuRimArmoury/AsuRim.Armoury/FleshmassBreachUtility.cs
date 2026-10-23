using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

internal static class FleshmassBreachUtility
{
	private const float SearchRadius = 120f;

	private static List<ThingDef> fleshmassDefs;

	private static readonly List<Thing> candidates = new List<Thing>();

	private static List<ThingDef> FleshmassDefs
	{
		get
		{
			if (fleshmassDefs != null)
			{
				return fleshmassDefs;
			}
			fleshmassDefs = new List<ThingDef>();
			List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
			for (int i = 0; i < allDefsListForReading.Count; i++)
			{
				ThingDef thingDef = allDefsListForReading[i];
				if (thingDef.building != null && (thingDef.HasComp(typeof(CompFleshmass)) || thingDef.HasComp(typeof(CompFleshmassBase))))
				{
					fleshmassDefs.Add(thingDef);
				}
			}
			return fleshmassDefs;
		}
	}

	private static bool IsFleshmassTarget(Thing t)
	{
		if (t?.def?.building == null || !t.def.building.isTargetable)
		{
			return false;
		}
		if (!t.def.destroyable)
		{
			return false;
		}
		if (!t.def.HasComp(typeof(CompFleshmass)))
		{
			return t.def.HasComp(typeof(CompFleshmassBase));
		}
		return true;
	}

	internal static Job TryGiveJob(Pawn pawn)
	{
		if (!ModsConfig.AnomalyActive)
		{
			return null;
		}
		Map map = pawn?.Map;
		if (map == null)
		{
			return null;
		}
		candidates.Clear();
		List<ThingDef> list = FleshmassDefs;
		for (int i = 0; i < list.Count; i++)
		{
			candidates.AddRange(map.listerThings.ThingsOfDef(list[i]));
		}
		if (candidates.Count == 0)
		{
			return null;
		}
		Thing thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, map, candidates, PathEndMode.Touch, TraverseParms.For(pawn), 120f, IsFleshmassTarget);
		candidates.Clear();
		if (thing == null)
		{
			return null;
		}
		Pawn_EquipmentTracker equipment = pawn.equipment;
		Job job = JobMaker.MakeJob((equipment != null && equipment.Primary?.def.IsRangedWeapon == true && pawn.equipment.PrimaryEq?.PrimaryVerb != null && pawn.equipment.PrimaryEq.PrimaryVerb.Available()) ? JobDefOf.AttackStatic : JobDefOf.AttackMelee, thing);
		job.expiryInterval = 60;
		job.checkOverrideOnExpire = true;
		return job;
	}
}
