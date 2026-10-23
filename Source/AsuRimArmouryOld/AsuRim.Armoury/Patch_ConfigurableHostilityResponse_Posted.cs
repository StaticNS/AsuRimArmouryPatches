using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse), "TryGiveJob")]
internal static class Patch_ConfigurableHostilityResponse_Posted
{
	private static bool Prefix(Pawn pawn, ref Job __result)
	{
		if (!ShouldOverride(pawn))
		{
			return true;
		}
		__result = null;
		return false;
	}

	private static bool ShouldOverride(Pawn pawn)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return false;
		}
		if (compArsenal.onDutyPost != null)
		{
			return true;
		}
		if (pawn.CurJobDef == ASA_PostJobDefOf.ASA_ManPost || pawn.CurJobDef == ASA_PostJobDefOf.ASA_Patrol)
		{
			return true;
		}
		if (Find.TickManager.TicksGame < compArsenal.combatProtectionUntil)
		{
			return true;
		}
		return IsRetreating(pawn, compArsenal);
	}

	private static bool IsRetreating(Pawn pawn, CompArsenal comp)
	{
		WeaponPolicy effectivePolicy = comp.EffectivePolicy;
		if (effectivePolicy == null || effectivePolicy.retreatAtHealth <= 0f)
		{
			return false;
		}
		if (pawn.health?.summaryHealth == null)
		{
			return false;
		}
		if (pawn.health.summaryHealth.SummaryHealthPercent > effectivePolicy.retreatAtHealth)
		{
			return false;
		}
		Area area = effectivePolicy.RetreatAreaFor(pawn);
		if (area == null)
		{
			return false;
		}
		return !area[pawn.Position];
	}
}
