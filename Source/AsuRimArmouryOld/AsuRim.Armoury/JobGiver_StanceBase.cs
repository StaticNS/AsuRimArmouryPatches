using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public abstract class JobGiver_StanceBase : JobGiver_AIFightEnemies
{
	internal const int CombatProtectionTicks = 300;

	protected virtual int ExpiryTicks => 60;

	protected override Job TryGiveJob(Pawn pawn)
	{
		Job job = TryRecallToArea(pawn);
		if (job != null)
		{
			return job;
		}
		Job job2 = PriorityJob(pawn);
		if (job2 != null)
		{
			return job2;
		}
		Pawn_PlayerSettings playerSettings = pawn.playerSettings;
		bool flag = playerSettings != null && (pawn.IsColonist || pawn.IsColonySubhuman);
		HostilityResponseMode hostilityResponse = (flag ? playerSettings.hostilityResponse : HostilityResponseMode.Flee);
		if (flag)
		{
			playerSettings.hostilityResponse = HostilityResponseMode.Attack;
		}
		try
		{
			Job job3 = base.TryGiveJob(pawn);
			if (job3 != null)
			{
				job3.expiryInterval = ExpiryTicks;
				CompArsenal compArsenal = CompArsenal.For(pawn);
				if (compArsenal != null)
				{
					compArsenal.combatProtectionUntil = Find.TickManager.TicksGame + 300;
				}
			}
			return job3;
		}
		finally
		{
			if (flag)
			{
				playerSettings.hostilityResponse = hostilityResponse;
			}
		}
	}

	protected virtual Job PriorityJob(Pawn pawn)
	{
		return null;
	}

	private static Job TryRecallToArea(Pawn pawn)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null || compArsenal.inMelee)
		{
			return null;
		}
		Area area = SquadManager.Current?.SquadOf(pawn)?.Area;
		if (area == null || area.Map != pawn.Map)
		{
			return null;
		}
		if (area[pawn.Position])
		{
			return null;
		}
		IntVec3 intVec = AreaUtility.ClosestCellIn(area, pawn);
		if (!intVec.IsValid)
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.Goto, intVec);
		job.locomotionUrgency = LocomotionUrgency.Sprint;
		job.expiryInterval = 60;
		job.checkOverrideOnExpire = true;
		return job;
	}

	protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
	{
		if (target is Pawn other && StanceTargeting.PassiveAnimal(other))
		{
			return false;
		}
		if (!StanceMovement.TargetAllowed(pawn, target))
		{
			return false;
		}
		return base.ExtraTargetValidator(pawn, target);
	}

	protected override bool ShouldLoseTarget(Pawn pawn)
	{
		if (base.ShouldLoseTarget(pawn))
		{
			return true;
		}
		Thing enemyTarget = pawn.mindState.enemyTarget;
		if (enemyTarget != null)
		{
			return !StanceMovement.TargetAllowed(pawn, enemyTarget);
		}
		return false;
	}

	protected override Job MeleeAttackJob(Pawn pawn, Thing enemyTarget)
	{
		if (StanceMovement.HoldsGun(pawn) && !StanceMovement.StanceOf(pawn).WantsMelee())
		{
			return null;
		}
		return base.MeleeAttackJob(pawn, enemyTarget);
	}

	protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
	{
		return StanceMovement.TryFindFiringCell(pawn, pawn.mindState.enemyTarget, verbToUse, 0f, out dest);
	}
}
