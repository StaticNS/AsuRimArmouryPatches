using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(ITab_Pawn_Gear), "InterfaceDrop")]
internal static class Patch_InterfaceDrop
{
	private static void Prefix(Thing t)
	{
		Pawn pawn = (t?.ParentHolder as Pawn_InventoryTracker)?.pawn ?? (t?.ParentHolder as Pawn_EquipmentTracker)?.pawn;
		if (pawn != null)
		{
			ArsenalActions.Forget(pawn, t);
		}
	}
}
