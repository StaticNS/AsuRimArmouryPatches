using System;
using ArmouryTactics.Compatability;
using AsuRim.Armoury;
using Verse;
using Verse.AI;

namespace ArmouryTactics;

internal static class RunAndGunStance
{
	internal static bool Active;

	internal static void Configure()
	{
		Active = (ModSettings_ArmouryTactics.runAndGunEnabled || ModSettings_ArmouryTactics.dualWieldEnabled) && !CombatExtendedCompat.Active;
	}

	internal static bool Intercept(Pawn_StanceTracker tracker, ref Stance newStance)
	{
		Pawn pawn = tracker.pawn;
		if (pawn == null || tracker != pawn.stances)
		{
			return false;
		}
		Type type = newStance.GetType();
		if (type == typeof(Stance_Warmup))
		{
			return Warmup(pawn, ref newStance);
		}
		if (type == typeof(Stance_Cooldown))
		{
			return Cooldown(tracker, pawn, (Stance_Cooldown)newStance);
		}
		return false;
	}

	private static bool Warmup(Pawn pawn, ref Stance newStance)
	{
		if (!ModSettings_ArmouryTactics.runAndGunEnabled)
		{
			return false;
		}
		Stance_Warmup stance_Warmup = (Stance_Warmup)newStance;
		Verb verb = stance_Warmup.verb;
		if (verb == null || verb.CasterPawn != pawn)
		{
			return false;
		}
		if (pawn.RaceProps == null || pawn.RaceProps.Animal)
		{
			return false;
		}
		Stance curStance = pawn.stances.curStance;
		if (curStance is Stance_RunAndGun_Cooldown { ticksLeft: >0 } && !OffHandFiring.Active(pawn))
		{
			return true;
		}
		if (pawn.RunsAndGuns())
		{
			Pawn_PathFollower pather = pawn.pather;
			if (pather != null && pather.Moving)
			{
				if (curStance is Stance_RunAndGun && !OffHandFiring.Active(pawn))
				{
					return false;
				}
				newStance = Convert(stance_Warmup);
				return false;
			}
		}
		return false;
	}

	private static Stance_RunAndGun Convert(Stance_Warmup warmup)
	{
		return new Stance_RunAndGun
		{
			ticksLeft = warmup.ticksLeft,
			startedTick = warmup.startedTick,
			verb = warmup.verb,
			focusTarg = warmup.focusTarg,
			neverAimWeapon = warmup.neverAimWeapon
		};
	}

	private static bool Cooldown(Pawn_StanceTracker stanceTracker, Pawn pawn, Stance_Cooldown stance)
	{
		Verb verb = stance.verb;
		if (verb == null || verb.EquipmentSource == null || !verb.verbProps.violent)
		{
			return false;
		}
		if (verb.CurrentTarget.TryGetPawn(out var pawn2) && pawn2 != null && pawn2.Faction != null && pawn2.Faction == pawn.Faction)
		{
			return false;
		}
		bool flag = ModSettings_ArmouryTactics.runAndGunEnabled && (pawn.RunsAndGuns() || stanceTracker.curStance is Stance_RunAndGun || stanceTracker.curStance is Stance_RunAndGun_Cooldown) && (pawn.pather?.Moving ?? false);
		bool result = false;
		if (ModSettings_ArmouryTactics.dualWieldEnabled && verb.EquipmentSource.IsOffHandedWeapon())
		{
			Pawn_StanceTracker offHandStanceTracker = pawn.GetOffHandStanceTracker();
			if (offHandStanceTracker != null)
			{
				if (flag)
				{
					offHandStanceTracker.SetStance(new Stance_RunAndGun_Cooldown(stance.ticksLeft, stance.focusTarg, verb));
				}
				else
				{
					offHandStanceTracker.SetStance(new Stance_Cooldown(stance.ticksLeft, stance.focusTarg, verb));
				}
				return true;
			}
		}
		if (flag)
		{
			stanceTracker.SetStance(new Stance_RunAndGun_Cooldown(stance.ticksLeft, stance.focusTarg, verb));
			result = true;
		}
		return result;
	}
}
