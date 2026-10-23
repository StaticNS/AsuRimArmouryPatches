using System;
using UnityEngine;
using Verse;

namespace AsuRimArmoury;

internal static class FlatUI
{
	internal static void Section(this Listing_Standard listing, string title)
	{
		listing.Gap(8f);
		Text.Font = GameFont.Medium;
		listing.Label(title);
		Text.Font = GameFont.Small;
		listing.GapLine(4f);
	}

	internal static void Note(this Listing_Standard listing, string text)
	{
		Color color = GUI.color;
		GUI.color = Color.gray;
		listing.Label(text);
		GUI.color = color;
		listing.Gap(2f);
	}

	internal static void Check(this Listing_Standard listing, string label, ref bool value, string tooltip = null, bool enabled = true, Action onChange = null)
	{
		Rect rect = listing.GetRect(Text.LineHeight);
		if (!tooltip.NullOrEmpty())
		{
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
			}
			TooltipHandler.TipRegion(rect, tooltip);
		}
		bool flag = value;
		Widgets.CheckboxLabeled(rect, label, ref value, !enabled);
		if (value != flag)
		{
			onChange?.Invoke();
		}
		listing.Gap(listing.verticalSpacing);
	}

	internal static void SliderRow(this Listing_Standard listing, string label, ref float value, float min, float max, float displayMult = 1f, int decimalPlaces = 0, string valueSuffix = "", string tooltip = null, bool enabled = true, Action onChange = null)
	{
		Color color = GUI.color;
		if (!enabled)
		{
			GUI.color = new Color(color.r, color.g, color.b, 0.4f);
		}
		listing.Label((TaggedString)(label + ": " + (value * displayMult).ToString("F" + decimalPlaces) + valueSuffix), -1f, tooltip);
		Rect rect = listing.GetRect(22f);
		if (enabled)
		{
			float num = value;
			value = Widgets.HorizontalSlider(rect, value, min, max);
			if (value != num)
			{
				onChange?.Invoke();
			}
		}
		else
		{
			Widgets.HorizontalSlider(rect, value, min, max);
		}
		GUI.color = color;
		listing.Gap(listing.verticalSpacing);
	}

	internal static void IntSliderRow(this Listing_Standard listing, string label, ref int value, int min, int max, string valueSuffix = "", string tooltip = null, bool enabled = true, Action onChange = null)
	{
		float value2 = value;
		listing.SliderRow(label, ref value2, min, max, 1f, 0, valueSuffix, tooltip, enabled, onChange);
		value = Mathf.RoundToInt(value2);
	}
}
