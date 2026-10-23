using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class OverwatchGizmo
{
	public static IEnumerable<Gizmo> For(Pawn pawn)
	{
		CompArsenal comp = CompArsenal.For(pawn);
		if (comp != null && StanceRules.CanTakeOrders(pawn) && pawn.Map != null)
		{
			Area overwatchArea = comp.OverwatchArea;
			bool flag = comp.overwatch && overwatchArea != null;
			yield return new Command_Action
			{
				defaultLabel = (flag ? overwatchArea.Label : ArmouryText.Get("ASA_Overwatch_Off")),
				defaultDesc = (flag ? ((string)"ASA_Overwatch_ArmedDesc".Translate(overwatchArea.Label)) : ArmouryText.Get("ASA_Overwatch_Desc")),
				icon = PostGizmos.OverwatchIcon(flag),
				groupKey = 84610003,
				action = () =>
				{
					Find.WindowStack.Add(new FloatMenu(Options(pawn, comp)));
				}
			};
		}
	}

	internal static List<FloatMenuOption> Options(Pawn pawn, CompArsenal comp)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>
		{
			new FloatMenuOption("ASA_Overwatch_Off".Translate(), () =>
			{
				comp.overwatch = false;
				comp.OverwatchArea = null;
			})
		};
		List<Area> allAreas = pawn.Map.areaManager.AllAreas;
		for (int num = 0; num < allAreas.Count; num++)
		{
			Area area = allAreas[num];
			if (area.AssignableAsAllowed())
			{
				list.Add(new FloatMenuOption(area.Label, () =>
				{
					comp.OverwatchArea = area;
					comp.overwatch = true;
					pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
				}));
			}
		}
		if (list.Count == 1)
		{
			list.Add(new FloatMenuOption("ASA_Overwatch_NoAreas".Translate(), null));
		}
		return list;
	}
}
