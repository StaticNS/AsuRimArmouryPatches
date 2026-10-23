using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
internal static class Candidates
{
	internal static readonly List<ThingDef> Melee;

	internal static readonly List<ThingDef> Ranged;

	static Candidates()
	{
		Melee = new List<ThingDef>();
		Ranged = new List<ThingDef>();
		List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			ThingDef thingDef = allDefsListForReading[i];
			if (thingDef.IsWeapon && !thingDef.destroyOnDrop && !thingDef.weaponTags.NullOrEmpty() && !(thingDef.generateCommonality <= 0f) && !(thingDef.BaseMarketValue <= 0f) && !ArmourySettings.InferTwoHanded(thingDef) && !thingDef.HasComp(typeof(CompBladelinkWeapon)))
			{
				if (thingDef.IsMeleeWeapon)
				{
					Melee.Add(thingDef);
				}
				else if (thingDef.IsRangedWeapon)
				{
					Ranged.Add(thingDef);
				}
			}
		}
	}
}
