using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public class LordToil_PlayerBreach : LordToil_AssaultColonyBreaching
{
	public IntVec3 breachTarget;

	private const int BreachRadius = 1;

	private const int WalkMargin = 2;

	public override void UpdateAllDuties()
	{
		if (!Data.breachDest.IsValid && breachTarget.IsValid && lord.ownedPawns.Any())
		{
			Data.Reset();
			Data.preferMelee = false;
			Data.breachStart = lord.ownedPawns[0].PositionHeld;
			Data.breachDest = breachTarget;
			Data.breachingGrid.CreateBreachPath(Data.breachStart, Data.breachDest, 1, 2);
		}
		base.UpdateAllDuties();
		PromoteMechanoidBreachers();
	}

	private void PromoteMechanoidBreachers()
	{
		bool flag = false;
		for (int num = base.pawnsEscort.Count - 1; num >= 0; num--)
		{
			Pawn pawn = base.pawnsEscort[num];
			if (pawn.RaceProps.IsMechanoid && BreachingUtility.FindVerbToUseForBreaching(pawn) != null)
			{
				base.pawnsEscort.RemoveAt(num);
				base.pawnsMeleeDestructive.Add(pawn);
				flag = true;
			}
		}
		if (flag)
		{
			LordToil_AssaultColonyBreaching.BalanceAndSetDuties(Data.breachDest, base.pawnsMeleeDestructive, base.pawnSoloAttackers, base.pawnsRangedDestructive, base.pawnsRangedGeneral, base.pawnsEscort);
		}
	}
}
