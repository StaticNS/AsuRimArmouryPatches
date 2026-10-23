using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace AsuRim.Armoury;

public class LordToil_PlayerBreach : LordToil_AssaultColonyBreaching
{
	public IntVec3 breachTarget;

	private const int BreachRadius = 1;

	private const int WalkMargin = 2;

	private static readonly FieldInfo pawnsEscortField = AccessTools.Field(typeof(LordToil_AssaultColonyBreaching), "pawnsEscort");

	private static readonly FieldInfo pawnsMeleeDestructiveField = AccessTools.Field(typeof(LordToil_AssaultColonyBreaching), "pawnsMeleeDestructive");

	private static readonly FieldInfo pawnSoloAttackersField = AccessTools.Field(typeof(LordToil_AssaultColonyBreaching), "pawnSoloAttackers");

	private static readonly FieldInfo pawnsRangedDestructiveField = AccessTools.Field(typeof(LordToil_AssaultColonyBreaching), "pawnsRangedDestructive");

	private static readonly FieldInfo pawnsRangedGeneralField = AccessTools.Field(typeof(LordToil_AssaultColonyBreaching), "pawnsRangedGeneral");

	private static readonly MethodInfo balanceAndSetDutiesMethod = AccessTools.Method(typeof(LordToil_AssaultColonyBreaching), "BalanceAndSetDuties");

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
		List<Pawn> pawnsEscort = pawnsEscortField?.GetValue(this) as List<Pawn>;
		List<Pawn> pawnsMeleeDestructive = pawnsMeleeDestructiveField?.GetValue(this) as List<Pawn>;
		bool flag = false;
		for (int num = pawnsEscort.Count - 1; num >= 0; num--)
		{
			Pawn pawn = pawnsEscort[num];
			if (pawn.RaceProps.IsMechanoid && BreachingUtility.FindVerbToUseForBreaching(pawn) != null)
			{
				pawnsEscort.RemoveAt(num);
				pawnsMeleeDestructive.Add(pawn);
				flag = true;
			}
		}
		if (flag)
		{
			balanceAndSetDutiesMethod?.Invoke(null, new object[]
			{
				Data.breachDest,
				pawnsMeleeDestructive,
				pawnSoloAttackersField?.GetValue(this),
				pawnsRangedDestructiveField?.GetValue(this),
				pawnsRangedGeneralField?.GetValue(this),
				pawnsEscort
			});
		}
	}
}
