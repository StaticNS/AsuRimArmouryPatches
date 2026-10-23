using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

internal static class ArmouryText
{
	private static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

	internal static string Get(string key)
	{
		if (!cache.TryGetValue(key, out var value))
		{
			value = key.Translate();
			cache[key] = value;
		}
		return value;
	}

	internal static string Label(this CombatStance stance)
	{
		return Get(stance.LabelKey());
	}

	internal static string Desc(this CombatStance stance)
	{
		return Get(stance.DescKey());
	}
}
