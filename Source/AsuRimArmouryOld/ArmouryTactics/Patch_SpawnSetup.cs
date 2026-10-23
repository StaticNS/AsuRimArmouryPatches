using HarmonyLib;
using Verse;

namespace ArmouryTactics;

[HarmonyPatch(typeof(Pawn), "SpawnSetup")]
internal class Patch_SpawnSetup
{
	private static bool Prepare()
	{
		return ModSettings_ArmouryTactics.runAndGunEnabled;
	}

	private static void Postfix(Pawn __instance)
	{
		if (!__instance.IsColonist && ModSettings_ArmouryTactics.enableForAI && ((__instance.RaceProps != null && __instance.RaceProps.intelligence != Intelligence.Animal) || ModSettings_ArmouryTactics.enableForAnimals))
		{
			__instance.SetRunsAndGuns(ModSettings_ArmouryTactics.CanRunAndGunAI(__instance));
		}
	}
}
