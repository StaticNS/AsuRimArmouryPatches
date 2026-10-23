using RimWorld;
using Verse;

namespace AsuRim.Armoury;

internal static class CombatMobilization
{
	internal static void Check(Pawn pawn, CompArsenal comp)
	{
		if (pawn?.drafter == null || comp == null || (comp.onDutyPost == null && !comp.autoDrafted && pawn.CurJobDef != ASA_PostJobDefOf.ASA_ManPost && pawn.CurJobDef != ASA_PostJobDefOf.ASA_Patrol && SquadManager.Current?.SquadOf(pawn) == null) || (GuardStandDown.Active && SurveillanceShiftBridge.OnGuardJob(pawn)))
		{
			return;
		}
		if (ThreatDetection.InRange(pawn, out var _) && WillFight(pawn))
		{
			if (!pawn.Drafted)
			{
				pawn.drafter.Drafted = true;
				comp.autoDrafted = true;
			}
		}
		else if (comp.autoDrafted && pawn.Drafted)
		{
			pawn.drafter.Drafted = false;
			comp.autoDrafted = false;
		}
	}

	private static bool WillFight(Pawn pawn)
	{
		HostilityResponseMode? hostilityResponseMode = pawn.playerSettings?.hostilityResponse;
		if (hostilityResponseMode.HasValue)
		{
			return hostilityResponseMode == HostilityResponseMode.Attack;
		}
		return true;
	}
}
