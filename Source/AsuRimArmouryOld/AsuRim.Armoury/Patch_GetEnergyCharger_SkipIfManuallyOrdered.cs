using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(JobGiver_GetEnergy_Charger), "TryGiveJob")]
internal static class Patch_GetEnergyCharger_SkipIfManuallyOrdered
{
	private const float CriticalFraction = 0.05f;

	private static bool Prefix(Pawn pawn, ref Job __result)
	{
		if (pawn == null || !pawn.Drafted)
		{
			return true;
		}
		Job curJob = pawn.CurJob;
		if (curJob == null || curJob.def != JobDefOf.Goto)
		{
			return true;
		}
		Need_MechEnergy need_MechEnergy = pawn.needs?.energy;
		if (need_MechEnergy == null || need_MechEnergy.CurLevelPercentage <= 0.05f)
		{
			return true;
		}
		__result = null;
		return false;
	}
}
