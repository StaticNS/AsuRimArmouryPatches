using RimWorld;
using Verse;

namespace AsuRim.Armoury;

internal static class Patch_MechPostCharge
{
	private const float VanillaChargePerTick = 0.00083333335f;

	internal static void Apply(Building_MechCharger __instance)
	{
		if (__instance.def != ASA_PostDefOf.ASA_MechWatchPost)
		{
			return;
		}
		Pawn currentlyChargingMech = __instance.CurrentlyChargingMech;
		if (currentlyChargingMech?.needs?.energy != null && __instance.Power != null && __instance.Power.PowerOn)
		{
			currentlyChargingMech.needs.energy.CurLevel -= 0.00041666668f;
			__instance.wasteProduced -= __instance.WasteProducedPerTick * 0.5f;
			if (__instance.wasteProduced < 0f)
			{
				__instance.wasteProduced = 0f;
			}
		}
	}
}
