using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
[GuardingPatch]
internal static class Patch_PlaySettings_GuardMarkers
{
	private static void Postfix(WidgetRow row, bool worldView)
	{
		if (!worldView && row != null && Current.ProgramState == ProgramState.Playing)
		{
			row.ToggleableIcon(ref GuardMarkerVisibility.ShowMarkers, StanceIcons.Get("UI/Stances/ASA_MarkerToggle", TexCommand.ForbidOff), "ASA_Post_ShowAll".Translate());
		}
	}
}
