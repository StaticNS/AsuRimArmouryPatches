using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ArmouryTactics;

public class ModSettings_ArmouryTactics : ModSettings
{
	public enum Tab
	{
		runAndGun,
		heavyWeapons,
		forbiddenWeapons,
		searchAndDestroy,
		dualWield,
		offHands,
		twoHanders,
		customRotations,
		offsets
	}

	public static bool enableForAI = true;

	public static bool enableForAnimals;

	public static bool runAndGunEnabled = true;

	public static bool searchAndDestroyEnabled = true;

	public static bool dualWieldEnabled = true;

	public static bool logging;

	public static bool meleeMirrored = true;

	public static bool rangedMirrored = true;

	public static int enableForFleeChance = 50;

	public static int NPCDualWieldChance = 10;

	public static float accuracyModifier = 0.65f;

	public static float accuracyModifierPlayer = 0.65f;

	public static float accuracyModifierMechs = 0.8f;

	public static float movementModifierHeavy = 0.3f;

	public static float movementModifierLight = 0.65f;

	public static float staticCooldownPOffHand = 20f;

	public static float staticCooldownPMainHand = 10f;

	public static float staticAccPOffHand = 10f;

	public static float staticAccPMainHand = 10f;

	public static float dynamicCooldownP = 5f;

	public static float dynamicAccP = 0.5f;

	public static float meleeAngle = 42f;

	public static float rangedAngle = 135f;

	public static float meleeXOffset = 0.24f;

	public static float rangedXOffset = 0.1f;

	public static float meleeZOffset = 0.09f;

	public static float rangedZOffset;

	public static HashSet<ushort> heavyWeaponsCache;

	public static HashSet<ushort> forbiddenWeaponsCache;

	public static HashSet<ushort> offHandersCache;

	public static HashSet<ushort> twoHandersCache;

	public static Dictionary<ushort, int> customRotationsCache;

	public static List<string> heavyWeapons;

	public static List<string> forbiddenWeapons;

	public static List<string> offHanders;

	public static List<string> twoHanders;

	public static Dictionary<string, int> customRotations;

	public static float weightLimitFilter = 3.4f;

	public const float weightLimitFilterDefault = 3.4f;

	public static Vector2 scrollPos;

	public static float cachedHeight;

	public static Tab selectedTab = Tab.runAndGun;

	public static bool VFECoreEnabled;

	public static Setup.OffHandShield OffHandShield;

	public static Setup.IsShield IsShield;

	public override void ExposeData()
	{
		Scribe_Values.Look(ref enableForAI, "enableForAI", defaultValue: true);
		Scribe_Values.Look(ref enableForAnimals, "enableForAnimals", defaultValue: false);
		Scribe_Values.Look(ref enableForFleeChance, "enableForFleeChance", 50);
		Scribe_Values.Look(ref accuracyModifier, "accuracyPenalty", 0.65f);
		Scribe_Values.Look(ref accuracyModifierPlayer, "accuracyPenaltyPlayer", 0.65f);
		Scribe_Values.Look(ref accuracyModifierMechs, "accuracyModifierMechs", 0.8f);
		Scribe_Values.Look(ref movementModifierHeavy, "movementModifierHeavy", 0.3f);
		Scribe_Values.Look(ref movementModifierLight, "movementModifierLight", 0.65f);
		Scribe_Collections.Look(ref heavyWeapons, "heavyWeapons", LookMode.Value);
		Scribe_Collections.Look(ref forbiddenWeapons, "forbiddenWeapons", LookMode.Value);
		Scribe_Collections.Look(ref offHanders, "offHanders", LookMode.Value);
		Scribe_Collections.Look(ref twoHanders, "twoHanders", LookMode.Value);
		Scribe_Collections.Look(ref customRotations, "customRotations", LookMode.Value, LookMode.Value);
		Scribe_Values.Look(ref meleeMirrored, "meleeMirrored", defaultValue: true);
		Scribe_Values.Look(ref rangedMirrored, "rangedMirrored", defaultValue: true);
		Scribe_Values.Look(ref staticCooldownPOffHand, "staticCooldownPOffHand", 20f);
		Scribe_Values.Look(ref staticCooldownPMainHand, "staticCooldownPMainHand", 10f);
		Scribe_Values.Look(ref staticAccPOffHand, "staticAccPOffHand", 10f);
		Scribe_Values.Look(ref staticAccPMainHand, "staticAccPMainHand", 10f);
		Scribe_Values.Look(ref dynamicCooldownP, "dynamicCooldownP", 5f);
		Scribe_Values.Look(ref dynamicAccP, "dynamicAccP", 0.5f);
		Scribe_Values.Look(ref meleeAngle, "meleeAngle", 42f);
		Scribe_Values.Look(ref rangedAngle, "rangedAngle", 135f);
		Scribe_Values.Look(ref meleeXOffset, "meleeXOffset", 0.24f);
		Scribe_Values.Look(ref rangedXOffset, "rangedXOffset", 0.1f);
		Scribe_Values.Look(ref meleeZOffset, "meleeZOffset", 0.09f);
		Scribe_Values.Look(ref rangedZOffset, "rangedZOffset", 0f);
		Scribe_Values.Look(ref NPCDualWieldChance, "NPCDualWieldChance", 10);
		Scribe_Values.Look(ref runAndGunEnabled, "runAndGunEnabled", defaultValue: true);
		Scribe_Values.Look(ref searchAndDestroyEnabled, "searchAndDestroyEnabled", defaultValue: true);
		Scribe_Values.Look(ref dualWieldEnabled, "dualWieldEnabled", defaultValue: true);
		base.ExposeData();
	}

	public static bool CanRunAndGunAI(Pawn pawn)
	{
		ThingWithComps thingWithComps = pawn.equipment?.Primary;
		pawn.GetOffHander(out var thing);
		if ((thingWithComps == null || !thingWithComps.def.IsWeaponUsingProjectiles) && (thing == null || !thing.def.IsWeaponUsingProjectiles))
		{
			return false;
		}
		if (!forbiddenWeaponsCache.Contains(thingWithComps?.def.shortHash ?? 0))
		{
			return !forbiddenWeaponsCache.Contains(thing?.def.shortHash ?? 0);
		}
		return false;
	}
}
