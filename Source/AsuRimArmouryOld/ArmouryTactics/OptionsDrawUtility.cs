using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArmouryTactics;

public static class OptionsDrawUtility
{
	public static int lineNumber;

	public static int cellPosition;

	public const int lineHeight = 22;

	public static string searchFilter = "";

	private static readonly Dictionary<string, string> truncatedLabelsCache = new Dictionary<string, string>();

	public static void DrawList(this Listing_Standard options, Rect rect)
	{
		lineNumber = (cellPosition = 0);
		for (int i = 0; i < Setup.allWeapons.Length; i++)
		{
			ThingDef thingDef = Setup.allWeapons[i];
			if (thingDef != null && (ModSettings_ArmouryTactics.selectedTab != ModSettings_ArmouryTactics.Tab.heavyWeapons || !(thingDef.BaseMass <= ModSettings_ArmouryTactics.weightLimitFilter)) && (ModSettings_ArmouryTactics.selectedTab != ModSettings_ArmouryTactics.Tab.offHands || (!(thingDef.BaseMass > ModSettings_ArmouryTactics.weightLimitFilter) && !ModSettings_ArmouryTactics.twoHandersCache.Contains(thingDef.shortHash))) && Matches(thingDef, searchFilter))
			{
				cellPosition += 22;
				lineNumber++;
				DrawListItem(options, thingDef);
			}
		}
	}

	private static bool Matches(ThingDef def, string filter)
	{
		if (filter.NullOrEmpty())
		{
			return true;
		}
		if (def.label != null && def.label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (def.defName != null && def.defName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string text = def.modContentPack?.Name;
		if (text != null)
		{
			return text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	public static void DrawListItem(Listing_Standard options, ThingDef def)
	{
		ushort shortHash = def.shortHash;
		bool checkOn;
		if (ModSettings_ArmouryTactics.selectedTab == ModSettings_ArmouryTactics.Tab.heavyWeapons)
		{
			checkOn = ModSettings_ArmouryTactics.heavyWeaponsCache.Contains(shortHash);
		}
		else if (ModSettings_ArmouryTactics.selectedTab == ModSettings_ArmouryTactics.Tab.forbiddenWeapons)
		{
			checkOn = ModSettings_ArmouryTactics.forbiddenWeaponsCache.Contains(shortHash);
		}
		else if (ModSettings_ArmouryTactics.selectedTab == ModSettings_ArmouryTactics.Tab.offHands)
		{
			checkOn = ModSettings_ArmouryTactics.offHandersCache.Contains(shortHash);
		}
		else
		{
			checkOn = ((ModSettings_ArmouryTactics.selectedTab != ModSettings_ArmouryTactics.Tab.twoHanders) ? ModSettings_ArmouryTactics.customRotationsCache.ContainsKey(shortHash) : ModSettings_ArmouryTactics.twoHandersCache.Contains(shortHash));
		}
		Rect rect = options.GetRect(22f);
		rect.y = cellPosition;
		string data = def.label + " :: " + def.modContentPack?.Name + " :: " + def.defName;
		if (!options.BoundingRectCached.HasValue || rect.Overlaps(options.BoundingRectCached.Value))
		{
			CheckboxLabeled(rect, data, def.label, ref checkOn, def);
		}
		if (lineNumber % 2 != 0)
		{
			Widgets.DrawLightHighlight(rect);
		}
		Widgets.DrawHighlightIfMouseover(rect);
		HashSet<ushort> hashSet;
		if (ModSettings_ArmouryTactics.selectedTab == ModSettings_ArmouryTactics.Tab.heavyWeapons)
		{
			hashSet = ModSettings_ArmouryTactics.heavyWeaponsCache;
		}
		else if (ModSettings_ArmouryTactics.selectedTab == ModSettings_ArmouryTactics.Tab.forbiddenWeapons)
		{
			hashSet = ModSettings_ArmouryTactics.forbiddenWeaponsCache;
		}
		else if (ModSettings_ArmouryTactics.selectedTab == ModSettings_ArmouryTactics.Tab.offHands)
		{
			hashSet = ModSettings_ArmouryTactics.offHandersCache;
		}
		else
		{
			if (ModSettings_ArmouryTactics.selectedTab != ModSettings_ArmouryTactics.Tab.twoHanders)
			{
				return;
			}
			hashSet = ModSettings_ArmouryTactics.twoHandersCache;
		}
		if (checkOn)
		{
			if (!hashSet.Contains(shortHash))
			{
				hashSet.Add(shortHash);
			}
		}
		else if (hashSet.Contains(shortHash))
		{
			hashSet.Remove(shortHash);
		}
	}

	private static void CheckboxLabeled(Rect rect, string data, string label, ref bool checkOn, ThingDef def)
	{
		Rect rect2 = rect.LeftHalf();
		Rect position = new Rect(rect2.x, rect2.y, 32f, rect2.height);
		Texture2D texture2D = null;
		if (def != null)
		{
			texture2D = def.uiIcon;
		}
		if (texture2D != null)
		{
			GUI.DrawTexture(position, texture2D, ScaleMode.ScaleToFit, alphaBlend: true, 1f, Color.white, 0f, 0f);
		}
		if (!label.NullOrEmpty())
		{
			Rect rect3 = new Rect(position.xMax, position.y, rect2.width - 32f, rect2.height);
			Widgets.Label(rect3, data?.Truncate(rect3.width - 12f, truncatedLabelsCache));
			Rect rect4 = rect.RightHalf();
			Widgets.Label(rect4, label.Truncate(rect4.width - 12f, truncatedLabelsCache));
		}
		else
		{
			Rect rect5 = new Rect(position.xMax, position.y, rect.width - 32f, rect2.height);
			Widgets.Label(rect5, data?.Truncate(rect5.width - 12f, truncatedLabelsCache));
		}
		if (Widgets.ButtonInvisible(rect))
		{
			checkOn = !checkOn;
			if (checkOn)
			{
				SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
				if (ModSettings_ArmouryTactics.selectedTab == ModSettings_ArmouryTactics.Tab.customRotations)
				{
					int startingValue = (int)def.equippedAngleOffset;
					Dialog_Slider window = new Dialog_Slider((int x) => "DW_Setting_CustomRotations_SetRotation".Translate(x), -180, 180, (int value) =>
					{
						ModSettings_ArmouryTactics.customRotationsCache[def.shortHash] = value;
						ModSettings_ArmouryTactics.customRotations[def.defName] = value;
					}, startingValue);
					Find.WindowStack.Add(window);
				}
			}
			else
			{
				if (ModSettings_ArmouryTactics.selectedTab == ModSettings_ArmouryTactics.Tab.customRotations)
				{
					if (ModSettings_ArmouryTactics.customRotationsCache.ContainsKey(def.shortHash))
					{
						ModSettings_ArmouryTactics.customRotationsCache.Remove(def.shortHash);
					}
					if (ModSettings_ArmouryTactics.customRotations.ContainsKey(def.defName))
					{
						ModSettings_ArmouryTactics.customRotations.Remove(def.defName);
					}
				}
				SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
			}
		}
		Widgets.CheckboxDraw(rect.xMax - 24f, rect.y, checkOn, disabled: false);
	}
}
