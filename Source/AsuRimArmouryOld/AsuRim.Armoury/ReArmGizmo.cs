using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public static class ReArmGizmo {
	public static IEnumerable<Gizmo> For(Pawn pawn) {
		CompArsenal comp = CompArsenal.For(pawn);
		if (comp != null && StanceRules.CanTakeOrders(pawn) && Arsenal.CanCarrySidearms(pawn)) {
			yield return new Command_Action {
				defaultLabel = ArmouryText.Get("ASA_Rearm"),
				defaultDesc = ArmouryText.Get("ASA_Rearm_Desc"),
				icon = StanceIcons.Get("UI/Stances/ASA_Rearm", TexCommand.RearmTrap),
				action = () => ReArm.Order(pawn)
			};
		}
	}
}
