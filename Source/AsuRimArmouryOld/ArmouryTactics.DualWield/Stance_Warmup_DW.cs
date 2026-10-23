using Verse;

namespace ArmouryTactics.DualWield;

internal class Stance_Warmup_DW : Stance_Warmup
{
	public override bool StanceBusy => true;

	public Stance_Warmup_DW()
	{
	}

	public Stance_Warmup_DW(int ticks, LocalTargetInfo focusTarg, Verb verb)
		: base(ticks, focusTarg, verb)
	{
	}

	public override void StanceDraw()
	{
		base.StanceDraw();
	}

	public override void StanceTick()
	{
		base.StanceTick();
		Pawn pawn = stanceTracker.pawn;
		if (!pawn.RunsAndGuns() && pawn.pather.MovingNow)
		{
			pawn.GetOffHandStanceTracker()?.SetStance(new Stance_Mobile());
		}
	}

	protected override void Expire()
	{
		verb.WarmupComplete();
		if (stanceTracker.curStance == this)
		{
			stanceTracker.pawn.GetOffHandStanceTracker()?.SetStance(new Stance_Mobile());
		}
	}
}
