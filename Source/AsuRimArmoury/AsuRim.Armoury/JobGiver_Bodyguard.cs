using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobGiver_Bodyguard : ThinkNode_JobGiver
{
	private const float SlotTolerance = 1.6f;

	private const int FollowExpiryTicks = 60;

	private const int WaitExpiryTicks = 60;

	public override Job TryGiveJob(Pawn pawn)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		Pawn pawn2 = compArsenal?.guarding;
		if (!Bodyguard.CanGuard(pawn, pawn2))
		{
			return null;
		}
		if (!GuardStandDown.BodyguardAllowed(pawn, pawn2))
		{
			return null;
		}
		bool drafted = pawn.Drafted;
		if (!drafted && Bodyguard.NeedsBreak(pawn))
		{
			return null;
		}
		bool flag = drafted && !HasStanceBranch(ArmouryStances.Effective(compArsenal));
		bool flag2 = pawn.Position.InHorDistOf(pawn2.Position, 7f);
		if (flag2 && Bodyguard.Fighting(pawn))
		{
			if (!flag)
			{
				return null;
			}
			return Wait(pawn);
		}
		IntVec3 intVec = Formations.SlotFor(pawn, pawn2);
		if (!intVec.IsValid)
		{
			if (!flag)
			{
				return null;
			}
			return Wait(pawn);
		}
		if (pawn.Position.DistanceTo(intVec) <= 1.6f)
		{
			if (!flag)
			{
				return null;
			}
			return Wait(pawn);
		}
		if (!pawn.CanReach(intVec, PathEndMode.OnCell, Danger.Deadly))
		{
			if (!flag)
			{
				return null;
			}
			return Wait(pawn);
		}
		Job job = JobMaker.MakeJob(JobDefOf.Goto, intVec);
		job.locomotionUrgency = (flag2 ? LocomotionUrgency.Jog : LocomotionUrgency.Sprint);
		job.expiryInterval = 60;
		job.checkOverrideOnExpire = true;
		return job;
	}

	private static bool HasStanceBranch(CombatStance stance)
	{
		if (stance != CombatStance.Hold)
		{
			return stance != CombatStance.Escort;
		}
		return false;
	}

	private static Job Wait(Pawn pawn)
	{
		Job job = JobMaker.MakeJob(JobDefOf.Wait_Combat, pawn.Position);
		job.expiryInterval = 60;
		job.checkOverrideOnExpire = true;
		return job;
	}
}
