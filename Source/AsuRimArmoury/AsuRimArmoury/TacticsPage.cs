using System;
using ArmouryTactics;
using ArmouryTactics.Compatability;
using AsuRim.Armoury;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRimArmoury;

internal static class TacticsPage
{
	private static Vector2 scrollPosition;

	private static float cachedHeight = 600f;

	internal static void Draw(Rect inRect)
	{
		bool flag = cachedHeight > inRect.height;
		Rect rect = new Rect(inRect);
		if (flag)
		{
			rect.width -= 20f;
			rect.height = cachedHeight;
			Widgets.BeginScrollView(inRect, ref scrollPosition, rect);
		}
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.maxOneColumn = true;
		listing_Standard.Begin(rect);
		listing_Standard.Section("Combat orders");
		listing_Standard.Note("Switching these off takes effect on restart.");
		listing_Standard.Check("Stance controls", ref ArmourySettings.stancesEnabled);
		listing_Standard.Check("Sidearms and ammo pickup", ref ArmourySettings.sidearmSystemEnabled);
		listing_Standard.Check("Armoury weapon panel", ref ArmourySettings.nativeLoadoutUIEnabled);
		listing_Standard.Check("Shoot the nearest threat", ref ArmourySettings.threatTargetingEnabled);
		listing_Standard.Check("Automatic weapon swaps", ref ArmourySettings.rangeDirectorEnabled);
		listing_Standard.SliderRow("Swap threshold (" + ArmourySettings.switchMargin.ToString("F2") + ")", ref ArmourySettings.switchMargin, 1f, 3f, 1f, 2, "", null, ArmourySettings.rangeDirectorEnabled);
		listing_Standard.IntSliderRow("Swap cooldown (" + ArmourySettings.switchCooldownTicks + " ticks)", ref ArmourySettings.switchCooldownTicks, 60, 1200, "", null, ArmourySettings.rangeDirectorEnabled);
		listing_Standard.Section("Target selection");
		listing_Standard.Note("Short-range weapons favour the closest target, long-range weapons favour the one they can actually hit. Replaces vanilla's own targeting.");
		listing_Standard.Check("Range-aware targeting", ref ArmourySettings.unifiedTargeting);
		listing_Standard.Note("Affects enemy AI. Requires a restart.");
		listing_Standard.Check("Apply to enemies and animals", ref ArmourySettings.unifiedTargetingForAI, null, ArmourySettings.unifiedTargeting);
		int value = (int)ArmourySettings.downedHandling;
		listing_Standard.IntSliderRow("Downed enemies (" + DownedLabel(ArmourySettings.downedHandling) + ")", ref value, 0, 2, "", null, ArmourySettings.unifiedTargeting);
		ArmourySettings.downedHandling = (DownedHandling)value;
		listing_Standard.Check("Tool switching", ref ArmourySettings.toolSwitching);
		listing_Standard.Check("Mute weapon swap sound", ref ArmourySettings.muteWeaponSwapSound);
		listing_Standard.Check("Sidearms for other factions", ref ArmourySettings.npcSidearms);
		listing_Standard.SliderRow("Chance (" + Mathf.RoundToInt(ArmourySettings.npcSidearmChance * 100f) + "%)", ref ArmourySettings.npcSidearmChance, 0f, 1f, 100f, 0, "%", null, ArmourySettings.npcSidearms);
		int value2 = (int)ArmourySettings.npcSidearmMinTechLevel;
		listing_Standard.IntSliderRow("Minimum tech (" + ((TechLevel)value2/*cast due to constrained. prefix*/).ToString() + ")", ref value2, 0, 7, "", null, ArmourySettings.npcSidearms);
		ArmourySettings.npcSidearmMinTechLevel = (TechLevel)value2;
		int value3 = (int)ArmourySettings.npcSidearmMaxTechLevel;
		listing_Standard.IntSliderRow("Maximum tech (" + ((TechLevel)value3/*cast due to constrained. prefix*/).ToString() + ")", ref value3, 0, 7, "", null, ArmourySettings.npcSidearms);
		ArmourySettings.npcSidearmMaxTechLevel = (TechLevel)value3;
		listing_Standard.Section("Walk-fire");
		listing_Standard.Note("Shoot while moving. Costs accuracy and speed.");
		listing_Standard.Check("Walk-fire", ref ArmourySettings.walkFireEnabled);
		listing_Standard.Indent();
		listing_Standard.ColumnWidth -= 12f;
		listing_Standard.Check("Default on", ref ArmourySettings.walkFireDefaultOn, null, ArmourySettings.walkFireEnabled);
		listing_Standard.Check("Enemies", ref ModSettings_ArmouryTactics.enableForAI, null, ArmourySettings.walkFireEnabled);
		listing_Standard.Check("Animals", ref ModSettings_ArmouryTactics.enableForAnimals, null, ArmourySettings.walkFireEnabled);
		listing_Standard.IntSliderRow("Flee chance (" + ModSettings_ArmouryTactics.enableForFleeChance + "%)", ref ModSettings_ArmouryTactics.enableForFleeChance, 0, 100, "%", null, ArmourySettings.walkFireEnabled && ModSettings_ArmouryTactics.enableForAI);
		listing_Standard.SliderRow("Player accuracy (" + Math.Round(ModSettings_ArmouryTactics.accuracyModifierPlayer * 100f) + "% of normal)", ref ModSettings_ArmouryTactics.accuracyModifierPlayer, 0f, 1f, 100f, 0, "%", null, ArmourySettings.walkFireEnabled);
		listing_Standard.SliderRow("Enemy accuracy (" + Math.Round(ModSettings_ArmouryTactics.accuracyModifier * 100f) + "% of normal)", ref ModSettings_ArmouryTactics.accuracyModifier, 0f, 1f, 100f, 0, "%", null, ArmourySettings.walkFireEnabled && ModSettings_ArmouryTactics.enableForAI);
		listing_Standard.SliderRow("Mech accuracy (" + Math.Round(ModSettings_ArmouryTactics.accuracyModifierMechs * 100f) + "% of normal)", ref ModSettings_ArmouryTactics.accuracyModifierMechs, 0f, 1f, 100f, 0, "%", null, ArmourySettings.walkFireEnabled);
		listing_Standard.SliderRow("Heavy weapon speed (" + Math.Round(ModSettings_ArmouryTactics.movementModifierHeavy * 100f) + "% of normal pace)", ref ModSettings_ArmouryTactics.movementModifierHeavy, 0f, 1f, 100f, 0, "%", null, ArmourySettings.walkFireEnabled);
		listing_Standard.SliderRow("Light weapon speed (" + Math.Round(ModSettings_ArmouryTactics.movementModifierLight * 100f) + "% of normal pace)", ref ModSettings_ArmouryTactics.movementModifierLight, 0f, 1f, 100f, 0, "%", null, ArmourySettings.walkFireEnabled);
		listing_Standard.SliderRow("Heavy threshold (" + ModSettings_ArmouryTactics.weightLimitFilter.ToString("F1") + " kg)", ref ModSettings_ArmouryTactics.weightLimitFilter, 0f, 10f, 1f, 1, " kg", null, ArmourySettings.walkFireEnabled);
		if (listing_Standard.ButtonText("Heavy weapons..."))
		{
			Find.WindowStack.Add(new Dialog_WeaponList(ModSettings_ArmouryTactics.Tab.heavyWeapons, "Heavy weapons"));
		}
		if (listing_Standard.ButtonText("Cannot fire while moving..."))
		{
			Find.WindowStack.Add(new Dialog_WeaponList(ModSettings_ArmouryTactics.Tab.forbiddenWeapons, "These weapons cannot be fired while moving."));
		}
		listing_Standard.ColumnWidth += 12f;
		listing_Standard.Outdent();
		listing_Standard.Section("Dual wielding");
		listing_Standard.Check("Dual wielding", ref ArmourySettings.dualWieldEnabled, null, enabled: true, () =>
		{
			ModSettings_ArmouryTactics.dualWieldEnabled = ArmourySettings.dualWieldEnabled;
		});
		listing_Standard.IntSliderRow("Chance for other factions (" + ModSettings_ArmouryTactics.NPCDualWieldChance + "%)", ref ModSettings_ArmouryTactics.NPCDualWieldChance, 0, 100, "%", null, ArmourySettings.dualWieldEnabled);
		listing_Standard.SliderRow("Two-handed threshold (" + ArmourySettings.twoHandedMassThreshold.ToString("F1") + " kg)", ref ArmourySettings.twoHandedMassThreshold, 1f, 6f, 1f, 1, " kg", null, ArmourySettings.dualWieldEnabled);
		listing_Standard.SliderRow("Main hand accuracy (" + Math.Round(ModSettings_ArmouryTactics.staticAccPMainHand) + ")", ref ModSettings_ArmouryTactics.staticAccPMainHand, 0f, 50f, 1f, 0, "", null, ArmourySettings.dualWieldEnabled);
		listing_Standard.SliderRow("Off hand accuracy (" + Math.Round(ModSettings_ArmouryTactics.staticAccPOffHand) + ")", ref ModSettings_ArmouryTactics.staticAccPOffHand, 0f, 50f, 1f, 0, "", null, ArmourySettings.dualWieldEnabled);
		listing_Standard.SliderRow("Accuracy per missing skill level (" + Math.Round(ModSettings_ArmouryTactics.dynamicAccP, 1) + ")", ref ModSettings_ArmouryTactics.dynamicAccP, 0f, 10f, 1f, 1, "", null, ArmourySettings.dualWieldEnabled);
		listing_Standard.SliderRow("Main hand cooldown (" + Math.Round(ModSettings_ArmouryTactics.staticCooldownPMainHand) + ")", ref ModSettings_ArmouryTactics.staticCooldownPMainHand, 0f, 300f, 1f, 0, "", null, ArmourySettings.dualWieldEnabled);
		listing_Standard.SliderRow("Off hand cooldown (" + Math.Round(ModSettings_ArmouryTactics.staticCooldownPOffHand) + ")", ref ModSettings_ArmouryTactics.staticCooldownPOffHand, 0f, 300f, 1f, 0, "", null, ArmourySettings.dualWieldEnabled);
		listing_Standard.SliderRow("Cooldown per missing skill level (" + Math.Round(ModSettings_ArmouryTactics.dynamicCooldownP) + ")", ref ModSettings_ArmouryTactics.dynamicCooldownP, 0f, 100f, 1f, 0, "", null, ArmourySettings.dualWieldEnabled);
		if (listing_Standard.ButtonText("Allowed in the off hand..."))
		{
			Find.WindowStack.Add(new Dialog_WeaponList(ModSettings_ArmouryTactics.Tab.offHands, "Allowed in the off hand"));
		}
		if (listing_Standard.ButtonText("Needs both hands..."))
		{
			Find.WindowStack.Add(new Dialog_WeaponList(ModSettings_ArmouryTactics.Tab.twoHanders, "These weapons need both hands and cannot be paired."));
		}
		listing_Standard.Section("Off-hand appearance");
		listing_Standard.Note("How the second weapon is drawn. Cosmetic.");
		listing_Standard.Check("Mirror melee", ref ModSettings_ArmouryTactics.meleeMirrored);
		listing_Standard.Check("Mirror ranged", ref ModSettings_ArmouryTactics.rangedMirrored);
		listing_Standard.SliderRow("Melee angle (" + Math.Round(ModSettings_ArmouryTactics.meleeAngle) + ")", ref ModSettings_ArmouryTactics.meleeAngle, 0f, 360f);
		listing_Standard.SliderRow("Ranged angle (" + Math.Round(ModSettings_ArmouryTactics.rangedAngle) + ")", ref ModSettings_ArmouryTactics.rangedAngle, 0f, 360f);
		listing_Standard.SliderRow("Melee horizontal offset (" + Math.Round(ModSettings_ArmouryTactics.meleeXOffset, 2) + ")", ref ModSettings_ArmouryTactics.meleeXOffset, -2f, 2f, 1f, 2);
		listing_Standard.SliderRow("Ranged horizontal offset (" + Math.Round(ModSettings_ArmouryTactics.rangedXOffset, 2) + ")", ref ModSettings_ArmouryTactics.rangedXOffset, -2f, 2f, 1f, 2);
		listing_Standard.SliderRow("Melee vertical offset (" + Math.Round(ModSettings_ArmouryTactics.meleeZOffset, 2) + ")", ref ModSettings_ArmouryTactics.meleeZOffset, -2f, 2f, 1f, 2);
		listing_Standard.SliderRow("Ranged vertical offset (" + Math.Round(ModSettings_ArmouryTactics.rangedZOffset, 2) + ")", ref ModSettings_ArmouryTactics.rangedZOffset, -2f, 2f, 1f, 2);
		if (listing_Standard.ButtonText("Per-weapon rotation..."))
		{
			Find.WindowStack.Add(new Dialog_WeaponList(ModSettings_ArmouryTactics.Tab.customRotations, "Click a weapon to set its own angle."));
		}
		if (Prefs.DevMode)
		{
			listing_Standard.Section("Debug");
			listing_Standard.Check("Logging", ref ModSettings_ArmouryTactics.logging);
		}
		if (GuardStandDown.Present)
		{
			listing_Standard.Section("ASA_SS_Section".Translate());
			listing_Standard.Check("ASA_SS_Integration".Translate(), ref ArmourySettings.surveillanceShiftIntegration, "ASA_SS_Integration_Desc".Translate());
			if (ArmourySettings.surveillanceShiftIntegration != GuardStandDown.Enabled)
			{
				listing_Standard.Note("ASA_SS_RestartNote".Translate());
			}
		}
		if (CombatExtendedCompat.Active)
		{
			listing_Standard.Section("Mod compatibility");
			listing_Standard.Note("Combat Extended loadouts");
			listing_Standard.Check("Let Combat Extended run loadouts instead of mine", ref ArmourySettings.standDownForCE);
		}
		else if (!GuardStandDown.Present)
		{
			listing_Standard.Section("Mod compatibility");
			listing_Standard.Note("No supported mods detected that need a compatibility switch here.");
		}
		cachedHeight = listing_Standard.CurHeight;
		listing_Standard.End();
		if (flag)
		{
			Widgets.EndScrollView();
		}
	}

	private static string DownedLabel(DownedHandling handling)
	{
		return handling switch
		{
			DownedHandling.Ignore => "Ignore", 
			DownedHandling.None => "No change", 
			_ => "Deprioritize", 
		};
	}
}
