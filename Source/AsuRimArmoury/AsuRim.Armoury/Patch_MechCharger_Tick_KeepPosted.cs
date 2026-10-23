using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Building_MechCharger), "Tick")]
[GuardingPatch]
internal static class Patch_MechCharger_Tick_KeepPosted
{
	private static void Prefix(Building_MechCharger __instance, out bool __state)
	{
		__state = false;
		Pawn currentlyChargingMech = __instance.CurrentlyChargingMech;
		if (currentlyChargingMech != null && MechPostCharging.IsPostedAt(currentlyChargingMech, __instance))
		{
			__state = true;
			currentlyChargingMech.CurJob.def = JobDefOf.MechCharge;
		}
	}

	private static Exception Finalizer(Exception __exception, Building_MechCharger __instance, bool __state)
	{
		if (__state)
		{
			Pawn currentlyChargingMech = __instance.CurrentlyChargingMech;
			if (currentlyChargingMech?.CurJob != null)
			{
				currentlyChargingMech.CurJob.def = ASA_PostJobDefOf.ASA_ManPost;
			}
		}
		if (__exception == null)
		{
			Patch_MechPostCharge.Apply(__instance);
		}
		return __exception;
	}
}
