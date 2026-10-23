using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(JobGiver_DropUnusedInventory), "Drop")]
internal static class Patch_DropUnusedInventory
{
	private static bool Prefix(Pawn pawn, Thing thing)
	{
		return !SidearmSafety.Protected(pawn, thing);
	}
}
