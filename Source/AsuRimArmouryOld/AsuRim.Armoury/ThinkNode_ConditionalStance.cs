using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class ThinkNode_ConditionalStance : ThinkNode_Conditional
{
	public CombatStance stance;

	public bool requiresDrafted = true;

	protected override bool Satisfied(Pawn pawn)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return false;
		}
		if (!ArmouryStances.Active(pawn))
		{
			return false;
		}
		if (requiresDrafted && !compArsenal.UnderCombatOrders)
		{
			return false;
		}
		if (!pawn.Drafted && NeedsCharge(pawn))
		{
			return false;
		}
		if (compArsenal.EffectiveStance != stance)
		{
			return false;
		}
		if (!pawn.Drafted && GuardStandDown.Active)
		{
			return !SurveillanceShiftBridge.HasDuty(pawn);
		}
		return true;
	}

	private static bool NeedsCharge(Pawn pawn)
	{
		Need_MechEnergy need_MechEnergy = pawn.needs?.energy;
		if (need_MechEnergy == null)
		{
			return false;
		}
		return need_MechEnergy.CurLevel + 0.1f < (float)JobGiver_GetEnergy.GetMinAutorechargeThreshold(pawn);
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		ThinkNode_ConditionalStance thinkNode_ConditionalStance = (ThinkNode_ConditionalStance)base.DeepCopy(resolve);
		thinkNode_ConditionalStance.stance = stance;
		thinkNode_ConditionalStance.requiresDrafted = requiresDrafted;
		return thinkNode_ConditionalStance;
	}
}
