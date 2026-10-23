using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryDropEquipment")]
internal static class Patch_NoteDroppedMainHand
{
	private static void Postfix(Pawn_EquipmentTracker __instance, bool __result, ref ThingWithComps resultingEq)
	{
		if (__result && resultingEq != null && resultingEq.def.equipmentType == EquipmentType.Primary)
		{
			KitRecovery.NoteMainDropped(__instance.pawn, resultingEq);
		}
	}
}
