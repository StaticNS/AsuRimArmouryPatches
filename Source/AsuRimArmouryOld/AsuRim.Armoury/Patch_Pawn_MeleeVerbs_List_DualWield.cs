using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_MeleeVerbs), "GetUpdatedAvailableVerbsList")]
internal static class Patch_Pawn_MeleeVerbs_List_DualWield
{
	private static void Postfix(Pawn_MeleeVerbs __instance, List<VerbEntry> __result)
	{
		if (!ArmouryTracked.OffHand.Contains(__instance.Pawn) || !ArmourySettings.dualWieldEnabled)
		{
			return;
		}
		int count = __result.Count;
		while (count-- > 0)
		{
			if (Arsenal.IsOffHand(__result[count].verb?.EquipmentSource))
			{
				__result.RemoveAt(count);
			}
		}
	}
}
