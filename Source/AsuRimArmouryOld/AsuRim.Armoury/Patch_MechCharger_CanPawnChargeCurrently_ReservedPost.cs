using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Building_MechCharger), "CanPawnChargeCurrently")]
[GuardingPatch]
internal static class Patch_MechCharger_CanPawnChargeCurrently_ReservedPost
{
	private static void Postfix(Building_MechCharger __instance, Pawn pawn, ref bool __result)
	{
		if (__result)
		{
			CompGuardPost compGuardPost = __instance.TryGetComp<CompGuardPost>();
			if (compGuardPost != null && compGuardPost.Kind == PostKind.Mech && compGuardPost.HasAnyone && !compGuardPost.IsAssigned(pawn))
			{
				__result = false;
			}
		}
	}
}
