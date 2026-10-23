using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(InspectTabBase), "DoTabGUI")]
[GuardingPatch]
internal static class Patch_DoTabGUI_NoCloseForPatrolQueue
{
	private static bool Prefix(InspectTabBase __instance)
	{
		ITab_PatrolQueue tab = __instance as ITab_PatrolQueue;
		if (tab == null)
		{
			return true;
		}
		Rect tabRect = tab.PatrolTabRect;
		Find.WindowStack.ImmediateWindow(235086, tabRect, WindowLayer.GameUI, () =>
		{
			if (!tab.PatrolTabStillValid || !tab.IsVisible)
			{
				return;
			}
			try
			{
				tab.FillPatrolTab();
			}
			catch (Exception ex)
			{
				Log.ErrorOnce("Exception filling tab " + tab.GetType()?.ToString() + ": " + ex, 49827);
			}
		}, doBackground: true, absorbInputAroundWindow: false, 1f, tab.Notify_ClickOutsideWindow);
		tab.DrawPatrolExtraOnGUI();
		return false;
	}
}
