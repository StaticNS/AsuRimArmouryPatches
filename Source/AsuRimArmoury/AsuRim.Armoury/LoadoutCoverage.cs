using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public static class LoadoutCoverage
{
	private const int NoQuality = int.MaxValue;

	private static readonly List<int> slotBuffer = new List<int>();

	private static readonly List<Thing> itemBuffer = new List<Thing>();

	private static readonly List<int> rankBuffer = new List<int>();

	public static bool Meets(Thing thing, QualityCategory min)
	{
		if ((int)min <= 0 || thing == null)
		{
			return true;
		}
		return Rank(thing) >= (int)min;
	}

	private static int Rank(Thing thing)
	{
		if (!thing.TryGetQuality(out var qc))
		{
			return int.MaxValue;
		}
		return (int)qc;
	}

	public static string Label(QualityCategory min)
	{
		if ((int)min <= 0)
		{
			return "AnyQuality".Translate().CapitalizeFirst();
		}
		return "ASA_Quality_OrBetter".Translate(min.GetLabel()).CapitalizeFirst();
	}

	public static void OpenPicker(Action<QualityCategory> set)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>
		{
			new FloatMenuOption(Label(QualityCategory.Awful), () =>
			{
				set(QualityCategory.Awful);
			})
		};
		foreach (QualityCategory allQualityCategory in QualityUtility.AllQualityCategories)
		{
			if ((int)allQualityCategory > 0)
			{
				QualityCategory choice = allQualityCategory;
				list.Add(new FloatMenuOption(Label(choice), () =>
				{
					set(choice);
				}));
			}
		}
		Find.WindowStack.Add(new FloatMenu(list));
	}

	public static bool Covers(Pawn pawn, Thing thing)
	{
		if (pawn == null || thing == null || !ArsenalRules.IsWieldable(thing))
		{
			return false;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null)
		{
			return false;
		}
		if (KitRecovery.Declined(compArsenal, thing))
		{
			return false;
		}
		if (IsRecorded(compArsenal, thing))
		{
			return true;
		}
		if (!Collect(pawn, compArsenal, thing.def))
		{
			return false;
		}
		Match();
		for (int i = 0; i < itemBuffer.Count; i++)
		{
			if (itemBuffer[i] == thing)
			{
				return rankBuffer[i] < 0;
			}
		}
		return false;
	}

	public static bool Unmet(Pawn pawn, ThingDef def, out QualityCategory min)
	{
		min = QualityCategory.Awful;
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal == null || def == null)
		{
			return false;
		}
		if (!Collect(pawn, compArsenal, def))
		{
			return false;
		}
		int num = Match();
		if (num < 0)
		{
			return false;
		}
		min = (QualityCategory)num;
		return true;
	}

	private static bool IsRecorded(CompArsenal comp, Thing thing)
	{
		if (comp.downMainThing == thing || comp.downOffThing == thing)
		{
			return true;
		}
		if (comp.downSidearms != null)
		{
			return comp.downSidearms.Contains(thing as ThingWithComps);
		}
		return false;
	}

	private static bool Collect(Pawn pawn, CompArsenal comp, ThingDef def)
	{
		slotBuffer.Clear();
		itemBuffer.Clear();
		rankBuffer.Clear();
		WeaponPolicy policy = comp.policy;
		if (policy != null)
		{
			if (policy.mainDef == def)
			{
				slotBuffer.Add((int)policy.mainMinQuality);
			}
			if (policy.offDef == def)
			{
				slotBuffer.Add((int)policy.offMinQuality);
			}
			for (int i = 0; i < policy.sidearms.Count; i++)
			{
				LoadoutSlot loadoutSlot = policy.sidearms[i];
				if (loadoutSlot != null && loadoutSlot.def == def)
				{
					slotBuffer.Add((int)loadoutSlot.minQuality);
				}
			}
		}
		int num = 0;
		for (int j = 0; j < comp.stowed.Count; j++)
		{
			if (comp.stowed[j] != null && comp.stowed[j].def == def)
			{
				num++;
			}
		}
		while (slotBuffer.Count < num)
		{
			slotBuffer.Add(0);
		}
		if (slotBuffer.Count == 0)
		{
			return false;
		}
		Add(pawn.equipment?.Primary, def);
		Add(Arsenal.OffHand(pawn), def);
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner != null)
		{
			for (int k = 0; k < thingOwner.Count; k++)
			{
				Add(thingOwner[k], def);
			}
		}
		return true;
	}

	private static void Add(Thing thing, ThingDef def)
	{
		if (thing != null && thing.def == def)
		{
			itemBuffer.Add(thing);
			rankBuffer.Add(Rank(thing));
		}
	}

	private static int Match()
	{
		slotBuffer.Sort((int a, int b) => b.CompareTo(a));
		int num = -1;
		for (int num2 = 0; num2 < slotBuffer.Count; num2++)
		{
			int num3 = slotBuffer[num2];
			int num4 = -1;
			for (int num5 = 0; num5 < rankBuffer.Count; num5++)
			{
				int num6 = rankBuffer[num5];
				if (num6 >= 0 && num6 >= num3 && (num4 < 0 || num6 > rankBuffer[num4]))
				{
					num4 = num5;
				}
			}
			if (num4 < 0)
			{
				if (num < 0)
				{
					num = num3;
				}
			}
			else
			{
				rankBuffer[num4] = -1;
			}
		}
		return num;
	}
}
