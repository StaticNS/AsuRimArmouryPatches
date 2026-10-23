using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ArmouryTactics;

[HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
internal static class Patch_TryStartMentalState
{
	private static bool Prepare()
	{
		return ModSettings_ArmouryTactics.runAndGunEnabled;
	}

	private static void Postfix(MentalStateHandler __instance, MentalStateDef stateDef)
	{
		if (stateDef == MentalStateDefOf.PanicFlee && ModSettings_ArmouryTactics.enableForAI && Rand.Range(1, 100) <= ModSettings_ArmouryTactics.enableForFleeChance)
		{
			__instance.pawn.SetRunsAndGuns(ModSettings_ArmouryTactics.CanRunAndGunAI(__instance.pawn));
		}
	}
}
