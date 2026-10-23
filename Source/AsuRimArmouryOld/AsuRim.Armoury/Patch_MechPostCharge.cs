using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

internal static class Patch_MechPostCharge
{
	private const float VanillaChargePerTick = 0.00083333335f;

	private static readonly FieldInfo wasteProducedField = AccessTools.Field(typeof(Building_MechCharger), "wasteProduced");

	private static readonly MethodInfo wasteProducedPerTickGetter = AccessTools.PropertyGetter(typeof(Building_MechCharger), "WasteProducedPerTick");

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
			float wasteProduced = (float)wasteProducedField.GetValue(__instance);
			float wasteProducedPerTick = (float)wasteProducedPerTickGetter.Invoke(__instance, null);
			wasteProduced -= wasteProducedPerTick * 0.5f;
			if (wasteProduced < 0f)
			{
				wasteProduced = 0f;
			}
			wasteProducedField.SetValue(__instance, wasteProduced);
		}
	}
}
