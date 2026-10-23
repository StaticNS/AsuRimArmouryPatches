using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ArmouryTactics;

[HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
internal static class Patch_TryStartMentalState
{
	private static readonly FieldInfo pawnField = AccessTools.Field(typeof(MentalStateHandler), "pawn");

	private static bool Prepare()
	{
		return ModSettings_ArmouryTactics.runAndGunEnabled;
	}

	private static void Postfix(MentalStateHandler __instance, MentalStateDef stateDef)
	{
		if (stateDef == MentalStateDefOf.PanicFlee && ModSettings_ArmouryTactics.enableForAI && Rand.Range(1, 100) <= ModSettings_ArmouryTactics.enableForFleeChance)
		{
			Pawn pawn = pawnField?.GetValue(__instance) as Pawn;
			if (pawn != null)
			{
				pawn.SetRunsAndGuns(ModSettings_ArmouryTactics.CanRunAndGunAI(pawn));
			}
		}
	}
}
