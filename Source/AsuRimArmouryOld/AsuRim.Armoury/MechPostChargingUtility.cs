using RimWorld;
using Verse;

namespace AsuRim.Armoury;

internal static class MechPostChargingUtility
{
	internal static void TryStart(Pawn mech, CompGuardPost post)
	{
		if (mech != null && post?.parent != null && post.parent is Building_MechCharger building_MechCharger && mech.RaceProps.IsMechanoid && mech.needs?.energy != null && building_MechCharger.CurrentlyChargingMech == null && mech.needs.energy.currentCharger == null && building_MechCharger.CanPawnChargeCurrently(mech))
		{
			building_MechCharger.StartCharging(mech);
		}
	}

	internal static void TryStop(Pawn mech, CompGuardPost post)
	{
		if (post?.parent is Building_MechCharger building_MechCharger && building_MechCharger.CurrentlyChargingMech == mech)
		{
			building_MechCharger.StopCharging();
		}
	}
}
