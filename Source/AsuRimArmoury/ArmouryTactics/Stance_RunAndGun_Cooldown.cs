using Verse;

namespace ArmouryTactics;

internal class Stance_RunAndGun_Cooldown : Stance_Cooldown
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

	public Stance_RunAndGun_Cooldown()
	{
	}

	public Stance_RunAndGun_Cooldown(int ticks, LocalTargetInfo focusTarg, Verb verb)
		: base(ticks, focusTarg, verb)
	{
	}
}
