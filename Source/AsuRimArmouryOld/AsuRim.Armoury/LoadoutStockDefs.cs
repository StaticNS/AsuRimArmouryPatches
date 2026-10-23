using System.Collections.Generic;
using Brutal.Enums;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
internal static class LoadoutStockDefs {
	private static readonly List<ThingDef> ammo;

	private static readonly List<ThingDef> grenades;

	private static readonly List<ThingDef> meals;

	private static readonly List<ThingDef> medicine;

	internal static bool AmmoAvailable {
		get {
			if (ammo.Count == 0) {
				return false;
			}
			if (!ModsConfig.IsActive("CETeam.CombatExtended")) {
				return ModsConfig.IsActive("ferny.ProgressionAmmunition");
			}
			return true;
		}
	}

	/* internal static bool GrenadesAvailable
	{
		get
		{
			if (grenades.Count == 0)
			{
				return false;
			}
			return ModsConfig.IsActive("CETeam.CombatExtended");
		}
	} */

	static LoadoutStockDefs() {
		ammo = new List<ThingDef>();
		grenades = new List<ThingDef>();
		meals = new List<ThingDef>();
		medicine = new List<ThingDef>();

		//Log.Message("[AsuRimArmoury] ");

		ThingCategoryDef ammoSilentFail = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Ammo");
		List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;

		for (int i = 0; i < allDefsListForReading.Count; i++) {
			ThingDef thingDef = allDefsListForReading[i];

			if (thingDef.IsApparel) continue;
			if (thingDef.IsMeleeWeapon) continue;

			if (thingDef.IsRangedWeapon) {
				//Log.Message("[AsuRimArmoury] Foundda wee-ah-pon");
				/* try {
					foreach (var cat in thingDef.weaponTags) {
						Log.Message("[AsuRimArmoury] " + cat);
					}
				} catch {
					Log.Message("[AsuRimArmoury] sowmthing went wong..");
				} */
				if (IsGrenade(thingDef)) {
					grenades.Add(thingDef);
				}
			} else {
				if (IsAmmo(thingDef, ammoSilentFail)) {
					ammo.Add(thingDef);
				} else if (IsMeal(thingDef)) {
					meals.Add(thingDef);
				} else if (IsMedicine(thingDef)) {
					medicine.Add(thingDef);
				}
			}
		}
		ammo.SortBy((ThingDef d) => d.label);
		grenades.SortBy((ThingDef d) => d.label);
		meals.SortBy((ThingDef d) => d.label);
		medicine.SortBy((ThingDef d) => d.label);
	}

	private static bool IsAmmo(ThingDef def, ThingCategoryDef ammoCategory) {
		if (def.thingCategories == null) {
			return false;
		}
		if (ammoCategory != null && def.thingCategories.Contains(ammoCategory)) {
			return true;
		}
		for (int i = 0; i < def.thingCategories.Count; i++) {
			string defName = def.thingCategories[i].defName;
			if (defName.StartsWith("Ammo") || defName.Contains("Ammo")) {
				return true;
			}
		}
		return false;
	}

	private static bool IsGrenade(ThingDef def) {
		if (def.weaponTags == null || def.weaponTags.Count == 0) return false;

		bool flag = false;
		foreach (string tag in def.weaponTags) {
			if (tag.Contains("Grenade")) {
				flag = true;
				if (tag.Contains("Launcher")) return false;
			}
		}
		return flag;
	}

	private static bool IsMeal(ThingDef def) {
		if (!def.IsNutritionGivingIngestible) {
			return false;
		}
		if (def.ingestible == null) {
			return false;
		}
		if (!def.ingestible.HumanEdible) {
			return false;
		}
		if (def.IsDrug) {
			return false;
		}
		return (int)def.ingestible.preferability >= 7;
	}

	private static bool IsMedicine(ThingDef def) {
		bool flag = false;

		flag |= def.IsMedicine;
		flag |= def.IsDrug;

		return flag;
	}

	internal static List<ThingDef> For(LoadoutStock kind) {
		switch (kind) {
			default: // implied LoadoutStock.Ammo
				return ammo;
			case LoadoutStock.Grenades:
				return grenades;
			case LoadoutStock.Meals:
				return meals;
			case LoadoutStock.Medicine:
				return medicine;
		}
	}

	internal static int DefaultCount(ThingDef def, LoadoutStock kind) {
		// Magic number!? Naughty!
		switch (kind) {
			default:
				return 1;
			case LoadoutStock.Ammo:
				return Mathf.Min(def.stackLimit, 30);
				//case LoadoutStock.Meals:
				//	return 2;
				//case LoadoutStock.Medicine:
				//	return 3;
		}
	}
}
