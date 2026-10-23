using HarmonyLib;
using Verse;

namespace ArmouryTactics;

[HarmonyPatch(typeof(Pawn), "TicksPerMove")]
internal class Patch_TicksPerMove
{
	private static bool Prepare()
	{
		return ModSettings_ArmouryTactics.runAndGunEnabled;
	}

	private static float Postfix(float __result, Pawn __instance)
	{
		if (__instance == null || __instance.stances == null)
		{
			return __result;
		}
		Stance curStance = __instance.stances.curStance;
		if (curStance is Stance_RunAndGun || curStance is Stance_RunAndGun_Cooldown)
		{
			float num = (ModSettings_ArmouryTactics.heavyWeaponsCache.Contains((__instance.equipment?.Primary?.def.shortHash).GetValueOrDefault()) ? ModSettings_ArmouryTactics.movementModifierHeavy : ModSettings_ArmouryTactics.movementModifierLight);
			if (num != 0f)
			{
				return __result / num;
			}
		}
		return __result;
	}
}
