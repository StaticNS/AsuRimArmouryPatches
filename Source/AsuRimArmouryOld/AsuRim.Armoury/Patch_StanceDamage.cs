using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Projectile))]
[HarmonyPatch("DamageAmount", MethodType.Getter)]
internal static class Patch_StanceDamage
{
	private static void Postfix(ref int __result, Thing ___launcher)
	{
		if (___launcher is Pawn pawn)
		{
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null && ArmouryStances.Effective(compArsenal) == CombatStance.Suppress)
			{
				__result = Math.Max(1, Mathf.RoundToInt((float)__result * 0.55f));
			}
		}
	}
}
