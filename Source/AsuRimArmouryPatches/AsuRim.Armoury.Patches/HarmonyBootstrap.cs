using System.Reflection;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury.Patches;

/// <summary>
/// This class is a static constructor that runs when the game starts. 
/// It uses Harmony to patch various methods in the AsuRim.Armoury mod to add or modify functionality. 
/// The patches include adding buttons to the loadout panel, adjusting the size of the policy window, 
///and modifying the margins of weapon-policy columns. 
/// If any of the target methods cannot be found, an error message is logged, and the corresponding feature is disabled.
/// Concise.
/// </summary>
[StaticConstructorOnStartup]
internal static class HarmonyBootstrap {

	/// <summary>
	/// It's the constructor. Wow.<br/><br/>
	/// I have no idea how any of this works. Magical. I just know it does.<br/>
	/// I don't need to know.
	/// </summary>
	static HarmonyBootstrap() {
		// Look at what all these programmers have been demanding respect from us all this time!
		// What even is any of this?

		Harmony harmony = new Harmony("StaticNS.AsuRimArmouryPatches");
		harmony.PatchAll(Assembly.GetExecutingAssembly());

		// LoadoutEditorUI.DrawFields is the method that draws the loadout editor window. 
		// We want to add our own fields to it, so we patch it with a prefix method.
		System.Type editorType = AccessTools.TypeByName("AsuRim.Armoury.LoadoutEditorUI");
		MethodInfo drawFields = editorType == null
			? null
			: AccessTools.Method(editorType, "DrawFields");
		if (drawFields == null) {
			Log.Error("[AsuRimArmouryPatches] Could not find LoadoutEditorUI.DrawFields; policy editor additions are disabled.");
		} else {
			harmony.Patch(drawFields, prefix: new HarmonyMethod(typeof(LoadoutEditorPatch), nameof(LoadoutEditorPatch.Prefix)));
		}

		// Gizmo_LoadoutPanel.DrawButtons is the method that draws the buttons on the loadout panel.
		// Same deal as above, we want to add our own buttons, so we patch it with a transpiler and a postfix method.
		System.Type panelType = AccessTools.TypeByName("AsuRim.Armoury.Gizmo_LoadoutPanel");
		MethodInfo drawButtons = panelType == null
			? null
			: AccessTools.Method(panelType, "DrawButtons");
		if (drawButtons == null) {
			Log.Error("[AsuRimArmouryPatches] Could not find Gizmo_LoadoutPanel.DrawButtons; the integrated rearm button is disabled.");
		} else {
			harmony.Patch(
				drawButtons,
				transpiler: new HarmonyMethod(typeof(ReArmPatch), nameof(ReArmPatch.PanelButtonsTranspiler)),
				postfix: new HarmonyMethod(typeof(ReArmPatch), nameof(ReArmPatch.PanelButtonsPostfix)));
			ReArmPatch.PanelButtonPatchInstalled = true;
		}

		// See above, but for Gizmo_LoadoutPanel.GetWidth. 
		// This is used to determine the width of the loadout panel, so we can adjust it to make room for our new button.
		MethodInfo panelWidth = panelType == null
			? null
			: AccessTools.Method(panelType, "GetWidth");
		if (panelWidth == null) {
			Log.Error("[AsuRimArmouryPatches] Could not find Gizmo_LoadoutPanel.GetWidth; the rearm button may overlap the collapse button.");
		} else {
			harmony.Patch(panelWidth, postfix: new HarmonyMethod(typeof(ReArmPatch), nameof(ReArmPatch.PanelWidthPostfix)));
		}

		// How many buttons are there? We need to know so we can adjust the width of the loadout panel accordingly.
		// We are audaciously adding a new one, after all.
		MethodInfo buttonCount = panelType == null ? null : AccessTools.PropertyGetter(panelType, "ButtonCount");
		if (buttonCount != null) {
			harmony.Patch(buttonCount, postfix: new HarmonyMethod(typeof(ReArmPatch), nameof(ReArmPatch.PanelButtonCountPostfix)));
		} else {
			Log.Error("[AsuRimArmouryPatches] Could not find Gizmo_LoadoutPanel.ButtonCount; the rearm button may overlap the loadout label.");
		}

		// The initial size of the policy window is too small for our new functionality.
		// Make it bigger. If you can't, brick the game.
		MethodInfo setInitialSize = AccessTools.Method(typeof(Window), "SetInitialSizeAndPosition");
		if (setInitialSize == null) {
			Log.Error("[AsuRimArmouryPatches] Could not find Window.SetInitialSizeAndPosition; the policy window size patch is disabled.");
		} else {
			harmony.Patch(
				setInitialSize,
				postfix: new HarmonyMethod(typeof(LoadoutEditorPatch), nameof(LoadoutEditorPatch.SetInitialSizeAndPositionPostfix)));
		}

		// Fix the fucking margins. Why does this need to be a fucking patch?>???
		System.Type policyColumnType = AccessTools.TypeByName("AsuRim.Armoury.PawnColumnWorker_WeaponPolicy");
		MethodInfo doHeader = policyColumnType == null ? null : AccessTools.Method(policyColumnType, "DoHeader");
		MethodInfo doCell = policyColumnType == null ? null : AccessTools.Method(policyColumnType, "DoCell");
		if (doHeader == null || doCell == null) {
			Log.Error("[AsuRimArmouryPatches] Could not find weapon-policy column methods; Assign-tab button margins are unchanged.");
		} else {
			HarmonyMethod headerPrefix = new HarmonyMethod(typeof(PolicyColumnMarginPatch), nameof(PolicyColumnMarginPatch.HeaderPrefix)) {
				priority = Priority.Last
			};
			HarmonyMethod cellPrefix = new HarmonyMethod(typeof(PolicyColumnMarginPatch), nameof(PolicyColumnMarginPatch.CellPrefix)) {
				priority = Priority.Last
			};
			harmony.Patch(doHeader, prefix: headerPrefix);
			harmony.Patch(doCell, prefix: cellPrefix);
		}
	}
}
