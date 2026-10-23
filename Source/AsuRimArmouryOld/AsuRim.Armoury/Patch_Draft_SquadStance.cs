using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_DraftController), "Drafted", MethodType.Setter)]
internal static class Patch_Draft_SquadStance
{
	private static void Postfix(Pawn_DraftController __instance)
	{
		Pawn pawn = __instance.pawn;
		if (!__instance.Drafted)
		{
			CompArsenal compArsenal = CompArsenal.For(pawn);
			if (compArsenal != null)
			{
				compArsenal.stanceLocked = false;
			}
			return;
		}
		Squad squad = SquadManager.Current?.SquadOf(pawn);
		if (squad != null && !squad.Stance.SquadOnly())
		{
			CompArsenal compArsenal2 = CompArsenal.For(pawn);
			if (compArsenal2 != null && compArsenal2.stance != squad.Stance && StanceRules.Allows(pawn, squad.Stance, out var _))
			{
				StanceGizmo.SetStance(pawn, compArsenal2, squad.Stance);
			}
		}
	}
}
