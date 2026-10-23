using System;
using System.Collections.Generic;
using Verse;

namespace AsuRimArmoury.Kit;

public static class CacheReset
{
	private static readonly List<Action> resets = new List<Action>();

	private static readonly List<Action> rebuilds = new List<Action>();

	public static void Register(Action reset)
	{
		if (reset != null && !resets.Contains(reset))
		{
			resets.Add(reset);
		}
	}

	public static void RegisterRebuild(Action rebuild)
	{
		if (rebuild != null && !rebuilds.Contains(rebuild))
		{
			rebuilds.Add(rebuild);
		}
	}

	internal static void RunResets()
	{
		Run(resets, "reset");
	}

	internal static void RunRebuilds()
	{
		Run(rebuilds, "rebuild");
	}

	private static void Run(List<Action> actions, string what)
	{
		for (int i = 0; i < actions.Count; i++)
		{
			try
			{
				actions[i]();
			}
			catch (Exception value)
			{
				Log.Error($"[{PatchGuard.Tag}] Cache {what} {actions[i].Method.DeclaringType?.Name}.{actions[i].Method.Name} threw: {value}");
			}
		}
	}
}
