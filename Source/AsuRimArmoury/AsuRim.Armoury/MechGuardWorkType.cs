using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
internal static class MechGuardWorkType
{
	static MechGuardWorkType()
	{
		if (GuardStandDown.Active)
		{
			return;
		}
		WorkTypeDef namedSilentFail = DefDatabase<WorkTypeDef>.GetNamedSilentFail("ASA_Guarding");
		if (namedSilentFail == null)
		{
			return;
		}
		List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
		int num = 0;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			RaceProperties race = allDefsListForReading[i].race;
			if (race != null && race.IsMechanoid)
			{
				if (race.mechEnabledWorkTypes == null)
				{
					race.mechEnabledWorkTypes = new List<WorkTypeDef>();
				}
				if (!race.mechEnabledWorkTypes.Contains(namedSilentFail))
				{
					race.mechEnabledWorkTypes.Add(namedSilentFail);
					num++;
				}
			}
		}
	}
}
