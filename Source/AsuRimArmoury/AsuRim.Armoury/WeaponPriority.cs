using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

public static class WeaponPriority
{
	public static bool Active(CompArsenal comp)
	{
		if (comp?.weaponOrder != null)
		{
			return comp.weaponOrder.Count > 0;
		}
		return false;
	}

	public static int Rank(CompArsenal comp, ThingDef def)
	{
		if (comp?.weaponOrder == null || def == null)
		{
			return int.MaxValue;
		}
		int num = comp.weaponOrder.IndexOf(def);
		if (num >= 0)
		{
			return num;
		}
		return int.MaxValue;
	}

	public static bool Before(CompArsenal comp, ThingWithComps a, float aScore, ThingWithComps b, float bScore)
	{
		if (b == null)
		{
			return a != null;
		}
		if (a == null)
		{
			return false;
		}
		int num = Rank(comp, a.def);
		int num2 = Rank(comp, b.def);
		if (num != num2)
		{
			return num < num2;
		}
		return aScore > bScore;
	}

	public static void Set(CompArsenal comp, List<ThingDef> order)
	{
		List<ThingDef> list = new List<ThingDef>();
		for (int i = 0; i < order.Count; i++)
		{
			if (order[i] != null && !list.Contains(order[i]))
			{
				list.Add(order[i]);
			}
		}
		comp.weaponOrder = list;
	}
}
