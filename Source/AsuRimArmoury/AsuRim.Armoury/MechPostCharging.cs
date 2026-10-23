using RimWorld;
using Verse;

namespace AsuRim.Armoury;

internal static class MechPostCharging
{
	internal static bool IsPostedAt(Pawn mech, Building_MechCharger charger)
	{
		if (mech?.CurJob == null || charger == null)
		{
			return false;
		}
		if (mech.CurJobDef != ASA_PostJobDefOf.ASA_ManPost)
		{
			return false;
		}
		return mech.CurJob.targetA.Thing == charger;
	}
}
