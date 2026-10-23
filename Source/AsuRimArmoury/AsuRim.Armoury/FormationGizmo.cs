using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class FormationGizmo
{
	public static IEnumerable<Gizmo> For(Pawn charge)
	{
		if (charge?.Map == null || (GuardStandDown.Active && !GuardStandDown.LeadsEscort(charge)))
		{
			yield break;
		}
		List<Pawn> list = Formations.DetailFor(charge);
		if (list.Count == 0)
		{
			yield break;
		}
		CompArsenal comp = CompArsenal.For(charge);
		if (comp == null)
		{
			yield break;
		}
		int count = list.Count;
		yield return new Command_Action
		{
			defaultLabel = ArmouryText.Get(comp.formation.LabelKey()),
			defaultDesc = "ASA_Formation_Desc".Translate(count) + "\n\n" + ArmouryText.Get(comp.formation.DescKey()),
			icon = StanceIcons.Get("UI/Stances/ASA_Formation_" + comp.formation, TexCommand.HoldOpen),
			action = () =>
			{
				List<FloatMenuOption> list2 = new List<FloatMenuOption>();
				foreach (GuardFormation value in Enum.GetValues(typeof(GuardFormation)))
				{
					if (value.Offerable())
					{
						GuardFormation captured = value;
						Texture2D iconTex = StanceIcons.Get("UI/Stances/ASA_Formation_" + captured, TexCommand.HoldOpen);
						Action<Rect> mouseoverGuiAction = (Rect rect) =>
						{
							TooltipHandler.TipRegion(rect, captured.LabelKey().Translate() + "\n\n" + captured.DescKey().Translate());
						};
						list2.Add(new FloatMenuOption(captured.LabelKey().Translate(), () =>
						{
							comp.formation = captured;
							List<Pawn> list3 = Formations.DetailFor(charge);
							for (int i = 0; i < list3.Count; i++)
							{
								list3[i].jobs?.EndCurrentJob(JobCondition.InterruptForced);
							}
						}, iconTex, Color.white, MenuOptionPriority.Default, mouseoverGuiAction));
					}
				}
				Find.WindowStack.Add(new FloatMenu(list2));
			}
		};
	}
}
