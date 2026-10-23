using HarmonyLib;
using RimWorld;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Dialog_FormCaravan), "DoWindowContents")]
internal static class Patch_FormCaravan_SquadRow
{
	private static void Prefix(Dialog_FormCaravan __instance)
	{
		CaravanSquadRow.Begin(__instance);
	}

	private static void Postfix()
	{
		CaravanSquadRow.End();
	}
}
