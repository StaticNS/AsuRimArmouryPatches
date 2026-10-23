using Verse;

namespace ArmouryTactics;

internal class Stance_RunAndGun : Stance_Warmup
{
	public override bool StanceBusy
	{
		get
		{
			if (Pawn != null)
			{
				return !(Pawn.pather?.Moving ?? false);
			}
			return false;
		}
	}

	public Stance_RunAndGun()
	{
	}

	public Stance_RunAndGun(int ticks, LocalTargetInfo focusTarg, Verb verb)
		: base(ticks, focusTarg, verb)
	{
	}
}
