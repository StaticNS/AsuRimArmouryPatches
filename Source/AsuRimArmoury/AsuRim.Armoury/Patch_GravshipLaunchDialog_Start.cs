using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Dialog_BeginGravshipLaunch), "Start")]
internal static class Patch_GravshipLaunchDialog_Start
{
	private static bool Prepare()
	{
		return ModsConfig.OdysseyActive;
	}

	private static void Prefix(Dialog_BeginGravshipLaunch __instance)
	{
		GravshipSquadBoarding.PendingSquads = GravshipSquadBoarding.SelectionFor(__instance);
	}
}
