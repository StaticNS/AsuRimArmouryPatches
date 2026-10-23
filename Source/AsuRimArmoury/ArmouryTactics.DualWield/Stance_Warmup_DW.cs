using UnityEngine;
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
		if (Find.Selector.IsSelected(stanceTracker.pawn))
		{
			Pawn pawn = stanceTracker.pawn;
			LocalTargetInfo localTargetInfo = focusTarg;
			float facing = 0f;
			if (localTargetInfo.Cell != pawn.Position)
			{
				facing = ((localTargetInfo.Thing != null) ? (localTargetInfo.Thing.DrawPos - pawn.Position.ToVector3Shifted()).AngleFlat() : (localTargetInfo.Cell - pawn.Position).AngleFlat);
			}
			float z = 0f;
			float x = 0f;
			switch (pawn.Rotation.AsInt)
			{
			case 1:
				z = 0.1f;
				break;
			case 3:
				z = -0.1f;
				break;
			case 2:
				x = -0.1f;
				break;
			default:
				x = -0.1f;
				break;
			}
			GenDraw.DrawAimPieRaw(pawn.DrawPos + new Vector3(x, 0.2f, z), facing, (int)((float)ticksLeft * pieSizeFactor));
		}
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

	public override void Expire()
	{
		verb.WarmupComplete();
		if (stanceTracker.curStance == this)
		{
			stanceTracker.pawn.GetOffHandStanceTracker()?.SetStance(new Stance_Mobile());
		}
	}
}
