using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
internal static class ToolStats
{
	private static readonly Dictionary<WorkTypeDef, StatDef> byWorkType;

	static ToolStats()
	{
		byWorkType = new Dictionary<WorkTypeDef, StatDef>();
		Map("Mining", "MiningSpeed");
		Map("Construction", "ConstructionSpeed");
		Map("Growing", "PlantWorkSpeed");
		Map("PlantCutting", "PlantWorkSpeed");
		Map("Cooking", "CookSpeed");
		Map("Smithing", "GeneralLaborSpeed");
		Map("Crafting", "GeneralLaborSpeed");
		Map("Tailoring", "GeneralLaborSpeed");
		Map("Art", "GeneralLaborSpeed");
		Map("Hauling", "CarryingCapacity");
		Map("Cleaning", "GeneralLaborSpeed");
		Map("Doctor", "MedicalTendSpeed");
	}

	private static void Map(string workType, string stat)
	{
		WorkTypeDef namedSilentFail = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workType);
		StatDef namedSilentFail2 = DefDatabase<StatDef>.GetNamedSilentFail(stat);
		if (namedSilentFail != null && namedSilentFail2 != null)
		{
			byWorkType[namedSilentFail] = namedSilentFail2;
		}
	}

	internal static StatDef For(Job job)
	{
		WorkTypeDef workTypeDef = job.workGiverDef?.workType;
		if (workTypeDef == null)
		{
			return null;
		}
		if (!byWorkType.TryGetValue(workTypeDef, out var value))
		{
			return null;
		}
		return value;
	}
}
