using System;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class StanceMovement
{
	public const float EngageRadius = 45f;

	public const float BrawlRadius = 30f;

	public const float HuntRadius = 45f;

	private const float MinStandoff = 6f;

	private const float StandoffFraction = 0.7f;

	public static CombatStance StanceOf(Pawn pawn)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null)
		{
			return ArmouryStances.Effective(compArsenal);
		}
		return CombatStance.Hold;
	}

	public static float SearchRadius(CombatStance stance)
	{
		switch (stance)
		{
		case CombatStance.Brawl:
			return 30f;
		case CombatStance.Hunt:
		case CombatStance.VIP:
		case CombatStance.Breach:
			return 45f;
		default:
			return 45f;
		}
	}

	public static float Standoff(CombatStance stance, float range)
	{
		if (stance != CombatStance.Engage && stance != CombatStance.Medic)
		{
			return 0f;
		}
		return Mathf.Min(Mathf.Max(range * 0.7f, 6f), range);
	}

	public static bool HoldsGun(Pawn pawn)
	{
		if (pawn == null)
		{
			return false;
		}
		return pawn.equipment?.Primary?.def.IsRangedWeapon == true;
	}

	public static Verb AttackVerb(Pawn pawn, Thing target, bool allowTurrets)
	{
		bool allowManualCastWeapons = !pawn.IsColonist && !pawn.IsColonySubhuman;
		return pawn.TryGetAttackVerb(target, allowManualCastWeapons, allowTurrets);
	}

	public static Area FenceArea(Pawn pawn)
	{
		Area area = SquadManager.Current?.SquadOf(pawn)?.Area;
		if (area == null || area.Map != pawn.Map)
		{
			return null;
		}
		return area;
	}

	public static Pawn FenceCharge(Pawn pawn)
	{
		Pawn pawn2 = CompArsenal.For(pawn)?.guarding;
		if (!Bodyguard.CanGuard(pawn, pawn2))
		{
			return null;
		}
		if (!GuardStandDown.BodyguardAllowed(pawn, pawn2))
		{
			return null;
		}
		return pawn2;
	}

	public static bool CellAllowed(Pawn pawn, IntVec3 cell)
	{
		return CellAllowed(cell, FenceArea(pawn), FenceCharge(pawn));
	}

	private static bool CellAllowed(IntVec3 cell, Area area, Pawn charge)
	{
		if (area != null && !area[cell])
		{
			return false;
		}
		if (charge != null && !cell.InHorDistOf(charge.Position, 7f))
		{
			return false;
		}
		return true;
	}

	public static bool TargetAllowed(Pawn pawn, Thing target)
	{
		if (target == null)
		{
			return false;
		}
		Area area = FenceArea(pawn);
		if (area != null && !area[target.Position])
		{
			return false;
		}
		Pawn pawn2 = FenceCharge(pawn);
		if (pawn2 == null)
		{
			return true;
		}
		float num = 1.5f;
		if (HoldsGun(pawn))
		{
			Verb verb = pawn.equipment.PrimaryEq?.PrimaryVerb;
			if (verb != null)
			{
				num = verb.EffectiveRange;
			}
		}
		return target.Position.InHorDistOf(pawn2.Position, 7f + num);
	}

	public static bool TryFindFiringCell(Pawn pawn, Thing target, Verb verb, float standoff, out IntVec3 dest)
	{
		dest = IntVec3.Invalid;
		if (pawn == null || target == null || !target.Spawned)
		{
			return false;
		}
		if (verb == null)
		{
			verb = AttackVerb(pawn, target, allowTurrets: false);
		}
		if (verb == null || verb.verbProps.IsMeleeAttack)
		{
			return false;
		}
		Area area = FenceArea(pawn);
		Pawn charge = FenceCharge(pawn);
		IntVec3 targetCell = target.Position;
		bool fenced = area != null || charge != null;
		Func<IntVec3, bool> validator = null;
		if ((standoff > 0f) | fenced)
		{
			validator = (IntVec3 c) => (standoff <= 0f || c.DistanceTo(targetCell) >= standoff) && (!fenced || CellAllowed(c, area, charge));
		}
		return CastPositionFinder.TryFindCastPosition(new CastPositionRequest
		{
			caster = pawn,
			target = target,
			verb = verb,
			maxRangeFromTarget = verb.EffectiveRange,
			wantCoverFromTarget = (verb.EffectiveRange > 5f),
			validator = validator
		}, out dest);
	}
}
