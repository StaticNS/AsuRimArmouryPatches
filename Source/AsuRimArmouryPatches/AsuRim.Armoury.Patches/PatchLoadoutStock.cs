using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury.Patches;

internal enum PatchLoadoutStock {
	Ammo,
	Grenades,
	Meals,
	Medicine
}

/// <summary>
/// Patch for the loadout editor to include our new selections: grenades and medicine.
/// The original mod creator believes adding a medicine tab is stupid. Well,, I think *you're* stupid. 
/// ... Jk I love you! Thanks for this amazing mod that's really cool and I want to bear it's children.
/// </summary>
internal static class PatchLoadoutStockDefs {
	private static List<ThingDef> ammo;
	private static List<ThingDef> grenades;
	private static List<ThingDef> meals;
	private static List<ThingDef> medicine;

	/// <summary>
	/// Combat Extended adds ammo. So, we need a special special field that tells us if there's ammo.
	/// You want to add ammo to your loadouts, right?
	/// </summary>
	internal static bool AmmoAvailable {
		get {
			EnsureInitialized();
			if (ammo.Count == 0) {
				return false;
			}
			if (!ModsConfig.IsActive("CETeam.CombatExtended")) {
				return ModsConfig.IsActive("ferny.ProgressionAmmunition");
			}
			return true;
		}
	}

	/// <summary>
	/// Returns our list corresponding to our stock we selected.
	/// This is such a lazy implementation. Better shit and alive then not shit and dead I guess.
	/// </summary>
	/// <param name="kind"></param>
	/// <returns></returns>
	internal static List<ThingDef> For(PatchLoadoutStock kind) {
		EnsureInitialized();
		switch (kind) {
			case PatchLoadoutStock.Grenades:
				return grenades;
			case PatchLoadoutStock.Meals:
				return meals;
			case PatchLoadoutStock.Medicine:
				return medicine;
			default:
				return ammo;
		}
	}

	/// <summary>
	/// What's our default count when we add a new item to our loadout?
	/// I don't think this matters.
	/// Here's an entire function for it.
	/// </summary>
	/// <param name="def"></param>
	/// <param name="kind"></param>
	/// <returns></returns>
	internal static int DefaultCount(ThingDef def, PatchLoadoutStock kind) {
		if (kind == PatchLoadoutStock.Ammo) {
			return Mathf.Min(def.stackLimit, 30);
		}
		return 1;
	}

	/// <summary>
	/// Init everything. Just for funsies. No real reason.
	/// But seriously, adds all items that we believe to be relevant to each stock.
	/// We're doing this ourselves even for the pre-existing stocks, just in case. 
	/// We might have differing ideas as to what a "grenade" might be. Idk
	/// </summary>
	private static void EnsureInitialized() {
		if (ammo != null) {
			return;
		}

		ammo = new List<ThingDef>();
		grenades = new List<ThingDef>();
		meals = new List<ThingDef>();
		medicine = new List<ThingDef>();
		ThingCategoryDef ammoCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Ammo");
		List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
		for (int i = 0; i < allDefs.Count; i++) {
			ThingDef def = allDefs[i];
			// none of our stock can be apparel or melee. 
			// so just skip the check if this def is one of the aformention things.
			// we could probably do more than just this.
			// I don't care enough to do more.
			if (def.IsApparel || def.IsMeleeWeapon) {
				continue;
			}

			if (def.IsRangedWeapon) {
				if (IsGrenade(def)) {
					grenades.Add(def);
				}
			} else if (IsAmmo(def, ammoCategory)) {
				ammo.Add(def);
			} else if (IsMeal(def)) {
				meals.Add(def);
			} else if (def.IsMedicine || def.IsDrug) {
				medicine.Add(def);
			}
		}

		ammo.SortBy(def => def.label);
		grenades.SortBy(def => def.label);
		meals.SortBy(def => def.label);
		medicine.SortBy(def => def.label);
	}

	/// <summary>
	/// Helper for checking if our def is an ammo. If that category already exists, great!
	/// If an item doesn't match up immediatly, let's check if there's other cats that include "Ammo" in the name.
	/// </summary>
	/// <param name="def"></param>
	/// <param name="ammoCategory"></param>
	/// <returns></returns>
	private static bool IsAmmo(ThingDef def, ThingCategoryDef ammoCategory) {
		if (def.thingCategories == null) {
			return false;
		}
		if (ammoCategory != null && def.thingCategories.Contains(ammoCategory)) {
			return true;
		}
		for (int i = 0; i < def.thingCategories.Count; i++) {
			string name = def.thingCategories[i].defName;
			if (name.StartsWith("Ammo") || name.Contains("Ammo")) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Helper for checking if a def is a grenade or not.
	/// Simply checking the tag for "Grenade" turns up things to shoot grenades too.
	/// That's no good. Make sure "Launcher" isn't in there too.
	/// </summary>
	/// <param name="def"></param>
	/// <returns></returns>
	private static bool IsGrenade(ThingDef def) {
		if (def.weaponTags == null) {
			return false;
		}
		bool grenade = false;
		for (int i = 0; i < def.weaponTags.Count; i++) {
			string tag = def.weaponTags[i];
			if (tag.Contains("Grenade")) {
				if (tag.Contains("Launcher")) {
					return false;
				}
				grenade = true;
			}
		}
		return grenade;
	}

	/// <summary>
	/// Helper to verify if something could be considered a meal.
	/// Funnily enough, incest- i mean insect jelly *is* a meal with this definition.
	/// </summary>
	/// <param name="def"></param>
	/// <returns></returns>
	private static bool IsMeal(ThingDef def) {
		return def.IsNutritionGivingIngestible
			&& def.ingestible != null
			&& def.ingestible.HumanEdible
			&& !def.IsDrug
			&& (int)def.ingestible.preferability >= 7;
	}
}
