using UnityEngine;
using Verse;

namespace AsuRim.Armoury.Patches;

/// <summary>
/// Patch to fix the margin issues in the Assign Tab. 
/// Without this, the loadout policy buttons are touching the buttons to the left and leave a huge gap on the right.
/// Again, why must this be a patch? Test your fucking mods! 
/// (I'm so mean I'm so sorry It's not like I'm testing this with much tenacity too)
/// </summary>
internal static class PolicyColumnMarginPatch {
	private static bool headerMarginLogged;
	private static bool cellMarginLogged;

	/// <summary>
	/// Debug shenanigans
	/// </summary>
	/// <param name="__0"></param>
	internal static void HeaderPrefix(ref Rect __0) {
		ApplyMargin(ref __0);
		if (!headerMarginLogged) {
			Log.Message($"[AsuRimArmouryPatches] Assign header rect after margin: {__0}.");
			headerMarginLogged = true;
		}
	}

	/// <summary>
	/// Debug shenanigans
	/// </summary>
	/// <param name="__0"></param>
	internal static void CellPrefix(ref Rect __0) {
		ApplyMargin(ref __0);
		if (!cellMarginLogged) {
			Log.Message($"[AsuRimArmouryPatches] Assign cell rect after margin: {__0}.");
			cellMarginLogged = true;
		}
	}

	/// <summary>
	/// Not debug shenanigans. This is called to apply a margin.
	/// </summary>
	/// <param name="rect"></param>
	private static void ApplyMargin(ref Rect rect) {
		rect.x += 7f;
		//rect.width = Mathf.Max(0f, rect.width - 16f);
	}
}
