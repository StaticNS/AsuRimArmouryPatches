using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public static class ArmourySettings
{
	public static bool stancesEnabled = true;

	public static bool sidearmSystemEnabled = true;

	public static bool threatTargetingEnabled = true;

	public static bool unifiedTargeting = true;

	public static bool unifiedTargetingForAI = true;

	public static DownedHandling downedHandling = DownedHandling.Deprioritize;

	public static bool dualWieldEnabled = true;

	public static bool nativeLoadoutUIEnabled = true;

	public static bool loadoutPanel = true;

	public static bool rangeDirectorEnabled = true;

	public static bool toolSwitching = true;

	public static bool muteWeaponSwapSound = true;

	public static bool walkFireEnabled = true;

	public static bool walkFireDefaultOn = true;

	public static bool standDownForCE = true;

	public static bool surveillanceShiftIntegration = true;

	public static bool npcSidearms = true;

	public static float npcSidearmChance = 0.35f;

	public static TechLevel npcSidearmMinTechLevel = TechLevel.Undefined;

	public static TechLevel npcSidearmMaxTechLevel = TechLevel.Archotech;

	public static float twoHandedMassThreshold = 2.6f;

	public static float switchMargin = 1.35f;

	public static int switchCooldownTicks = 240;

	public static bool InferTwoHanded(ThingDef def)
	{
		if (def == null)
		{
			return false;
		}
		if (def.IsMeleeWeapon)
		{
			return def.BaseMass > twoHandedMassThreshold;
		}
		if (def.weaponTags != null)
		{
			for (int i = 0; i < def.weaponTags.Count; i++)
			{
				string text = def.weaponTags[i];
				if (text == "Bow" || text == "NeolithicRangedHeavy")
				{
					return true;
				}
			}
		}
		return def.BaseMass > twoHandedMassThreshold;
	}

	public static void ExposeData()
	{
		Scribe_Values.Look(ref stancesEnabled, "asa_stancesEnabled", defaultValue: true);
		Scribe_Values.Look(ref sidearmSystemEnabled, "asa_sidearmSystemEnabled", defaultValue: true);
		Scribe_Values.Look(ref threatTargetingEnabled, "asa_threatTargetingEnabled", defaultValue: true);
		Scribe_Values.Look(ref unifiedTargeting, "asa_unifiedTargeting", defaultValue: true);
		Scribe_Values.Look(ref unifiedTargetingForAI, "asa_unifiedTargetingForAI", defaultValue: true);
		Scribe_Values.Look(ref downedHandling, "asa_downedHandling", DownedHandling.Deprioritize);
		Scribe_Values.Look(ref dualWieldEnabled, "asa_dualWieldEnabled", defaultValue: true);
		Scribe_Values.Look(ref nativeLoadoutUIEnabled, "asa_nativeLoadoutUIEnabled", defaultValue: true);
		Scribe_Values.Look(ref loadoutPanel, "asa_loadoutPanel", defaultValue: true);
		Scribe_Values.Look(ref toolSwitching, "asa_toolSwitching", defaultValue: true);
		Scribe_Values.Look(ref muteWeaponSwapSound, "asa_muteWeaponSwapSound", defaultValue: true);
		Scribe_Values.Look(ref walkFireEnabled, "asa_walkFireEnabled", defaultValue: true);
		Scribe_Values.Look(ref walkFireDefaultOn, "asa_walkFireDefaultOn", defaultValue: true);
		Scribe_Values.Look(ref standDownForCE, "asa_standDownForCE", defaultValue: true);
		Scribe_Values.Look(ref surveillanceShiftIntegration, "asa_surveillanceShiftIntegration", defaultValue: true);
		Scribe_Values.Look(ref npcSidearms, "asa_npcSidearms", defaultValue: true);
		Scribe_Values.Look(ref npcSidearmChance, "asa_npcSidearmChance", 0.35f);
		Scribe_Values.Look(ref npcSidearmMinTechLevel, "asa_npcSidearmMinTechLevel", TechLevel.Undefined);
		Scribe_Values.Look(ref npcSidearmMaxTechLevel, "asa_npcSidearmMaxTechLevel", TechLevel.Archotech);
		Scribe_Values.Look(ref rangeDirectorEnabled, "asa_rangeDirectorEnabled", defaultValue: true);
		Scribe_Values.Look(ref twoHandedMassThreshold, "asa_twoHandedMassThreshold", 2.6f);
		Scribe_Values.Look(ref switchMargin, "asa_switchMargin", 1.35f);
		Scribe_Values.Look(ref switchCooldownTicks, "asa_switchCooldownTicks", 240);
	}
}
