using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class AnimalGuardGizmo
{
	public static IEnumerable<Gizmo> For(Pawn pawn)
	{
		if (GuardStandDown.Active || !pawn.RaceProps.Animal || !StanceRules.AnimalCanBeCommanded(pawn) || pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			yield break;
		}
		CompArsenal comp = CompArsenal.For(pawn);
		if (comp == null)
		{
			yield break;
		}
		Pawn charge = comp.guarding;
		yield return new Command_Action
		{
			defaultLabel = ((charge != null) ? ((string)"ASA_Bodyguard_Stop".Translate(charge.LabelShort)) : ArmouryText.Get("ASA_Bodyguard_Gizmo")),
			defaultDesc = ArmouryText.Get("ASA_Bodyguard_Gizmo_Desc"),
			icon = StanceIcons.Get("UI/Stances/ASA_Escort", BaseContent.BadTex),
			action = () =>
			{
				if (charge != null)
				{
					comp.guarding = null;
					pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
				}
				else
				{
					Find.Targeter.BeginTargeting(TargetingParameters.ForPawns(), (LocalTargetInfo target) =>
					{
						if (target.Thing is Pawn { Faction: not null } pawn2 && pawn2.Faction.IsPlayer && Bodyguard.CanGuard(pawn, pawn2))
						{
							comp.guarding = pawn2;
							pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
						}
					}, pawn);
				}
			}
		};
	}
}
