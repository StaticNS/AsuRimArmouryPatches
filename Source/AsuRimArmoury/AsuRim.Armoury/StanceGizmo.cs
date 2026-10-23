using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class StanceGizmo
{
	public static IEnumerable<Gizmo> For(Pawn pawn)
	{
		CompArsenal comp = CompArsenal.For(pawn);
		if (comp != null && StanceRules.CanTakeOrders(pawn) && ArmouryStances.HasStances(pawn))
		{
			yield return new Command_Action
			{
				defaultLabel = comp.stance.Label(),
				defaultDesc = BuildDescription(comp.stance),
				icon = StanceIcons.Get(comp.stance),
				hotKey = KeyBindingDefOf.Misc4,
				groupKey = 84610001,
				action = () =>
				{
					Find.WindowStack.Add(new FloatMenu(Options(pawn, comp)));
				}
			};
		}
	}

	private static string BuildDescription(CombatStance current)
	{
		return current.Label() + "\n\n" + current.Desc();
	}

	internal static List<FloatMenuOption> Options(Pawn pawn, CompArsenal comp)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		foreach (CombatStance value in Enum.GetValues(typeof(CombatStance)))
		{
			if (!value.Offerable() || value.SquadOnly() || !ArmouryStances.Offers(pawn, value))
			{
				continue;
			}
			CombatStance choice = value;
			Texture2D iconTex = StanceIcons.Get(choice);
			Action<Rect> mouseoverGuiAction = (Rect rect) =>
			{
				TooltipHandler.TipRegion(rect, BuildDescription(choice));
			};
			if (!StanceRules.Allows(pawn, choice, out var reason))
			{
				list.Add(new FloatMenuOption(choice.LabelKey().Translate() + " (" + reason + ")", null, iconTex, Color.white, MenuOptionPriority.Default, mouseoverGuiAction));
				continue;
			}
			list.Add(new FloatMenuOption(choice.LabelKey().Translate(), () =>
			{
				List<Pawn> list2 = Commanded.With(pawn);
				for (int i = 0; i < list2.Count; i++)
				{
					Pawn pawn2 = list2[i];
					CompArsenal compArsenal = CompArsenal.For(pawn2);
					if (compArsenal != null && ArmouryStances.Offers(pawn2, choice) && StanceRules.Allows(pawn2, choice, out var _))
					{
						SetStance(pawn2, compArsenal, choice, manual: true);
					}
				}
			}, iconTex, Color.white, MenuOptionPriority.Default, mouseoverGuiAction));
		}
		return list;
	}

	public static void SetStance(Pawn pawn, CompArsenal comp, CombatStance stance, bool manual = false)
	{
		if ((manual || !comp.stanceLocked) && comp.stance != stance)
		{
			CombatStance stance2 = comp.stance;
			if (stance2 == CombatStance.Breach)
			{
				PlayerBreaching.Release(pawn);
			}
			comp.stance = stance;
			if (manual)
			{
				comp.stanceLocked = pawn.Drafted;
			}
			if (stance == CombatStance.Breach)
			{
				PlayerBreaching.AssignTo(pawn);
			}
			if (stance == CombatStance.Brawl || stance2 == CombatStance.Brawl)
			{
				RangeDirector.SyncBrawl(pawn);
			}
			pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
		}
	}
}
