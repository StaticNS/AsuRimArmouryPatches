using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_EquipmentTracker), "Notify_EquipmentAdded")]
internal static class Patch_RecheckOffHandOnEquip
{
	private static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
	{
		if (eq == null || eq.def.equipmentType != EquipmentType.Primary)
		{
			return;
		}
		Pawn pawn = __instance.pawn;
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return;
		}
		if (compArsenal.downMainThing != null)
		{
			Job curJob = pawn.CurJob;
			if (compArsenal.downMainThing == eq || (curJob != null && curJob.playerForced && curJob.def == JobDefOf.Equip))
			{
				compArsenal.downMainThing = null;
			}
		}
		if (compArsenal.declined != null && compArsenal.declined.Count > 0)
		{
			KitRecovery.Undecline(compArsenal, eq);
		}
		if (compArsenal.offHand != null)
		{
			ArsenalActions.CheckOffHand(pawn, compArsenal);
		}
	}
}
