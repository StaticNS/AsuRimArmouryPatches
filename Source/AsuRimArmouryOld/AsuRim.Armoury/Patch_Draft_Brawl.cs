using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_DraftController), "Drafted", MethodType.Setter)]
internal static class Patch_Draft_Brawl
{
	private static void Postfix(Pawn_DraftController __instance)
	{
		Pawn pawn = __instance.pawn;
		if (pawn != null && ArmourySettings.stancesEnabled)
		{
			RangeDirector.SyncBrawl(pawn);
		}
	}
}
