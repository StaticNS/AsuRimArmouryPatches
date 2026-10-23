using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

internal static class ToolSwitching
{
	internal static void OnJobStart(Pawn pawn, Job job)
	{
		if (!ArmourySettings.toolSwitching || job == null || pawn == null || pawn.Drafted || !Arsenal.CanCarrySidearms(pawn) || !pawn.IsColonist || pawn.mindState?.enemyTarget != null)
		{
			return;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null || compArsenal.forced != null || compArsenal.forcedUnarmed)
		{
			return;
		}
		StatDef statDef = ToolStats.For(job);
		if (statDef == null)
		{
			Restore(pawn, compArsenal);
			return;
		}
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		float num = Score(thingWithComps?.def, statDef);
		ThingWithComps thingWithComps2 = null;
		float num2 = num;
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner == null)
		{
			return;
		}
		for (int i = 0; i < thingOwner.Count; i++)
		{
			if (thingOwner[i] is ThingWithComps thingWithComps3 && ArsenalRules.CanAutoWield(pawn, thingWithComps3))
			{
				float num3 = Score(thingWithComps3.def, statDef);
				if (num3 > num2)
				{
					num2 = num3;
					thingWithComps2 = thingWithComps3;
				}
			}
		}
		if (thingWithComps2 != null)
		{
			if (compArsenal.beforeTool == null && thingWithComps != null)
			{
				compArsenal.beforeTool = new LoadoutSlot(thingWithComps);
			}
			ArsenalActions.StowOffHand(pawn);
			ArsenalActions.EquipMain(pawn, thingWithComps2);
		}
	}

	private static void Restore(Pawn pawn, CompArsenal comp)
	{
		LoadoutSlot beforeTool = comp.beforeTool;
		if (beforeTool == null)
		{
			return;
		}
		comp.beforeTool = null;
		if (beforeTool.Matches(Arsenal.MainHand(pawn)))
		{
			return;
		}
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner == null)
		{
			return;
		}
		for (int i = 0; i < thingOwner.Count; i++)
		{
			if (thingOwner[i] is ThingWithComps thingWithComps && beforeTool.Matches(thingWithComps) && ArsenalRules.CanAutoWield(pawn, thingWithComps))
			{
				ArsenalActions.EquipMain(pawn, thingWithComps);
				break;
			}
		}
	}

	private static float Score(ThingDef def, StatDef stat)
	{
		if (def?.equippedStatOffsets == null)
		{
			return 0f;
		}
		return def.equippedStatOffsets.GetStatOffsetFromList(stat);
	}
}
