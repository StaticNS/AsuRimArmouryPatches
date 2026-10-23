using System;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(VerbProperties))]
[HarmonyPatch("AdjustedCooldown")]
[HarmonyPatch(new Type[]
{
	typeof(Verb),
	typeof(Pawn)
})]
internal static class Patch_StanceCooldown
{
	private static float Postfix(float __result, VerbProperties __instance, Pawn attacker)
	{
		if (__instance.IsMeleeAttack)
		{
			return __result;
		}
		CompArsenal compArsenal = CompArsenal.For(attacker);
		if (compArsenal == null || ArmouryStances.Effective(compArsenal) != CombatStance.Suppress)
		{
			return __result;
		}
		return __result * 0.6f;
	}
}
