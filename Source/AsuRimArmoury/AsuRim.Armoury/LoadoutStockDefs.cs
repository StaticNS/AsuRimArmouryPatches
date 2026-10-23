using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
internal static class LoadoutStockDefs
{
	private static readonly List<ThingDef> ammo;

	private static readonly List<ThingDef> meals;

	internal static bool AmmoAvailable
	{
		get
		{
			if (ammo.Count == 0)
			{
				return false;
			}
			if (!ModsConfig.IsActive("CETeam.CombatExtended"))
			{
				return ModsConfig.IsActive("ferny.ProgressionAmmunition");
			}
			return true;
		}
	}

	static LoadoutStockDefs()
	{
		ammo = new List<ThingDef>();
		meals = new List<ThingDef>();
		ThingCategoryDef namedSilentFail = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Ammo");
		List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			ThingDef thingDef = allDefsListForReading[i];
			if (!thingDef.IsWeapon && !thingDef.IsApparel)
			{
				if (IsAmmo(thingDef, namedSilentFail))
				{
					ammo.Add(thingDef);
				}
				else if (IsMeal(thingDef))
				{
					meals.Add(thingDef);
				}
			}
		}
		ammo.SortBy((ThingDef d) => d.label);
		meals.SortBy((ThingDef d) => d.label);
	}

	private static bool IsAmmo(ThingDef def, ThingCategoryDef ammoCategory)
	{
		if (def.thingCategories == null)
		{
			return false;
		}
		if (ammoCategory != null && def.thingCategories.Contains(ammoCategory))
		{
			return true;
		}
		for (int i = 0; i < def.thingCategories.Count; i++)
		{
			string defName = def.thingCategories[i].defName;
			if (defName.StartsWith("Ammo") || defName.Contains("Ammo"))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsMeal(ThingDef def)
	{
		if (!def.IsNutritionGivingIngestible)
		{
			return false;
		}
		if (def.ingestible == null)
		{
			return false;
		}
		if (!def.ingestible.HumanEdible)
		{
			return false;
		}
		if (def.IsDrug)
		{
			return false;
		}
		return (int)def.ingestible.preferability >= 7;
	}

	internal static List<ThingDef> For(LoadoutStock kind)
	{
		if (kind != LoadoutStock.Ammo)
		{
			return meals;
		}
		return ammo;
	}

	internal static int DefaultCount(ThingDef def, LoadoutStock kind)
	{
		if (kind == LoadoutStock.Meals)
		{
			return 2;
		}
		return Mathf.Min(def.stackLimit, 100);
	}
}
