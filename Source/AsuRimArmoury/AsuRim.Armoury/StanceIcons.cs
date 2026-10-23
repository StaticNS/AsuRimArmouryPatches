using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
public static class StanceIcons
{
	private static readonly Dictionary<CombatStance, Texture2D> cache = new Dictionary<CombatStance, Texture2D>();

	private static readonly Dictionary<string, Texture2D> named = new Dictionary<string, Texture2D>();

	public static Texture2D Get(CombatStance stance)
	{
		if (cache.TryGetValue(stance, out var value))
		{
			return value;
		}
		value = ContentFinder<Texture2D>.Get(stance.IconPath(), reportFailure: false) ?? BaseContent.BadTex;
		cache[stance] = value;
		return value;
	}

	public static Texture2D Get(string path, Texture2D fallback)
	{
		if (named.TryGetValue(path, out var value))
		{
			return value;
		}
		value = ContentFinder<Texture2D>.Get(path, reportFailure: false) ?? fallback;
		named[path] = value;
		return value;
	}
}
