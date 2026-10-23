using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(RitualBehaviorWorker_GravshipLaunch), "TryExecuteOn")]
internal static class Patch_GravshipLaunchExecute_SquadBoarding
{
	private static bool Prepare()
	{
		return ModsConfig.OdysseyActive;
	}

	private static void Prefix(TargetInfo target, RitualRoleAssignments assignments)
	{
		HashSet<Squad> pendingSquads = GravshipSquadBoarding.PendingSquads;
		if (pendingSquads == null || pendingSquads.Count == 0)
		{
			return;
		}
		foreach (Squad item in pendingSquads)
		{
			List<Pawn> members = item.Members;
			for (int i = 0; i < members.Count; i++)
			{
				Pawn pawn = members[i];
				if (pawn != null && pawn.RaceProps.Humanlike && !pawn.Dead && !pawn.Downed && pawn.Map == target.Map)
				{
					assignments.TryAssignSpectate(pawn);
				}
			}
		}
	}

	private static void Postfix(TargetInfo target)
	{
		HashSet<Squad> pendingSquads = GravshipSquadBoarding.PendingSquads;
		GravshipSquadBoarding.PendingSquads = null;
		if (pendingSquads == null || pendingSquads.Count == 0)
		{
			return;
		}
		Building_GravEngine building_GravEngine = target.Thing?.TryGetComp<CompPilotConsole>()?.engine;
		if (building_GravEngine == null)
		{
			return;
		}
		Building_GravEngine building_GravEngine2 = building_GravEngine;
		if (building_GravEngine2.pawnsToBoard == null)
		{
			building_GravEngine2.pawnsToBoard = new HashSet<Pawn>();
		}
		foreach (Squad item in pendingSquads)
		{
			List<Pawn> members = item.Members;
			for (int i = 0; i < members.Count; i++)
			{
				Pawn pawn = members[i];
				if (pawn != null && !pawn.RaceProps.Humanlike && !pawn.Dead && !pawn.Downed && pawn.Map == target.Map)
				{
					building_GravEngine.pawnsToBoard.Add(pawn);
					pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
				}
			}
		}
	}
}
