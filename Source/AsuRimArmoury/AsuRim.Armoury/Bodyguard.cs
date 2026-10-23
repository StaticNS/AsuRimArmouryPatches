using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class Bodyguard
{
	public const float GuardRadius = 4.5f;

	public const float RecallRadius = 7f;

	public static bool CanGuard(Pawn guard, Pawn charge)
	{
		if (guard == null || charge == null || guard == charge)
		{
			return false;
		}
		if (charge.Dead || charge.Destroyed || !charge.Spawned)
		{
			return false;
		}
		if (charge.Map != guard.Map)
		{
			return false;
		}
		return true;
	}

	public static bool Fighting(Pawn guard)
	{
		Thing thing = guard.mindState?.enemyTarget;
		if (thing == null || !thing.Spawned || thing.Destroyed || thing.Map != guard.Map)
		{
			return false;
		}
		if (thing is Pawn pawn && (pawn.Dead || pawn.Downed))
		{
			return false;
		}
		return true;
	}

	public static bool NeedsBreak(Pawn guard)
	{
		Pawn_NeedsTracker needs = guard.needs;
		if (needs == null)
		{
			return false;
		}
		if (needs.food != null && (int)needs.food.CurCategory >= 1)
		{
			return true;
		}
		if (needs.rest != null && (int)needs.rest.CurCategory >= 1)
		{
			return true;
		}
		if (guard.health?.hediffSet != null && guard.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Initial))
		{
			return true;
		}
		MentalBreaker mentalBreaker = guard.mindState?.mentalBreaker;
		if (needs.joy != null && mentalBreaker != null && mentalBreaker.CurMood < mentalBreaker.BreakThresholdMinor)
		{
			return true;
		}
		return false;
	}
}
