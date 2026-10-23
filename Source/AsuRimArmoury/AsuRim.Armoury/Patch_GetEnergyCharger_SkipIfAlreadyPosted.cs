using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(JobGiver_GetEnergy_Charger), "TryGiveJob")]
[GuardingPatch]
internal static class Patch_GetEnergyCharger_SkipIfAlreadyPosted
{
	private static bool Prefix(Pawn pawn, ref Job __result)
	{
		Building_MechCharger building_MechCharger = pawn?.needs?.energy?.currentCharger;
		if (building_MechCharger == null || !MechPostCharging.IsPostedAt(pawn, building_MechCharger))
		{
			return true;
		}
		__result = null;
		return false;
	}
}
