using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Thing), "TakeDamage")]
internal static class Patch_StanceTakeDamage
{
	private const float ImmobiliseLegChance = 0.6f;

	private static void Prefix(Thing __instance, ref DamageInfo dinfo)
	{
		if (!(__instance is Pawn pawn) || !(dinfo.Instigator is Pawn pawn2))
		{
			return;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn2);
		if (compArsenal == null || ArmouryStances.Effective(compArsenal) != CombatStance.Immobilise || !Rand.Chance(0.6f))
		{
			return;
		}
		HediffSet hediffSet = pawn.health?.hediffSet;
		if (hediffSet == null)
		{
			return;
		}
		foreach (BodyPartRecord notMissingPart in hediffSet.GetNotMissingParts())
		{
			if (notMissingPart.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore))
			{
				dinfo.SetHitPart(notMissingPart);
				break;
			}
		}
	}
}
