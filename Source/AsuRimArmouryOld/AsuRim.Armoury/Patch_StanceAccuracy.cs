using ArmouryTactics;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(VerbProperties), "AdjustedAccuracy")]
internal static class Patch_StanceAccuracy
{
	private static bool tactics;

	private static bool Prepare()
	{
		tactics = ModSettings_ArmouryTactics.runAndGunEnabled || ModSettings_ArmouryTactics.dualWieldEnabled;
		return true;
	}

	private static float Postfix(float __result, VerbProperties __instance, Thing equipment)
	{
		if (!(equipment?.ParentHolder is Pawn_EquipmentTracker { pawn: var pawn }))
		{
			return __result;
		}
		if (pawn == null)
		{
			return __result;
		}
		if (tactics)
		{
			__result = TacticsAccuracy(__result, __instance, equipment, pawn);
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return __result;
		}
		if (ArmouryStances.Effective(compArsenal) == CombatStance.Immobilise)
		{
			return __result * 0.8f;
		}
		if (__instance.IsMeleeAttack)
		{
			return __result;
		}
		if (ArmouryStances.Effective(compArsenal) != CombatStance.Suppress)
		{
			return __result;
		}
		return __result * 0.65f;
	}

	private static float TacticsAccuracy(float result, VerbProperties props, Thing equipment, Pawn pawn)
	{
		if (ModSettings_ArmouryTactics.runAndGunEnabled)
		{
			Stance stance = pawn.stances?.curStance;
			if (stance is Stance_RunAndGun || stance is Stance_RunAndGun_Cooldown)
			{
				result *= (pawn.IsColonist ? ModSettings_ArmouryTactics.accuracyModifierPlayer : ModSettings_ArmouryTactics.accuracyModifier);
			}
		}
		if (ModSettings_ArmouryTactics.dualWieldEnabled)
		{
			int num = ((pawn.skills == null) ? 8 : (props.IsMeleeAttack ? pawn.skills.GetSkill(SkillDefOf.Melee) : pawn.skills.GetSkill(SkillDefOf.Shooting)).levelInt);
			float num2 = (Arsenal.IsOffHand(equipment) ? ModSettings_ArmouryTactics.staticAccPOffHand : ModSettings_ArmouryTactics.staticAccPMainHand) / 100f;
			result *= 1f - (num2 - ModSettings_ArmouryTactics.dynamicAccP / 100f * (float)(20 - num));
		}
		return result;
	}
}
