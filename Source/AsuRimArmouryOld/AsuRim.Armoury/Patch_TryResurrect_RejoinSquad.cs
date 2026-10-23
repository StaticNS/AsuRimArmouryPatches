using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(ResurrectionUtility), "TryResurrect")]
internal static class Patch_TryResurrect_RejoinSquad
{
	private static void Postfix(bool __result, Pawn pawn)
	{
		if (__result && pawn != null && pawn.RaceProps.IsMechanoid && pawn.Faction != null && pawn.Faction.IsPlayer)
		{
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null && compArsenal.lastSquadId >= 0)
			{
				Squad squad = SquadManager.Current?.GetById(compArsenal.lastSquadId);
				compArsenal.lastSquadId = -1;
				squad?.Add(pawn);
			}
		}
	}
}
