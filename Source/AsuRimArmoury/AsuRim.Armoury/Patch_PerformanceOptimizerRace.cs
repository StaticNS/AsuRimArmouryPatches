using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
internal static class Patch_PerformanceOptimizerRace
{
	static Patch_PerformanceOptimizerRace()
	{
		if (ModsConfig.IsActive("Taranchuk.PerformanceOptimizer"))
		{
			Harmony harmony = new Harmony("asunib.RulesOfEngagement.RenderRaceCompat");
			MethodInfo methodInfo = AccessTools.Method(typeof(InvisibilityUtility), "GetAlpha");
			if (methodInfo != null)
			{
				harmony.Patch(methodInfo, null, null, null, new HarmonyMethod(typeof(Patch_PerformanceOptimizerRace), "FinalizeAlpha"));
			}
			MethodInfo methodInfo2 = AccessTools.Method(typeof(InvisibilityUtility), "IsHiddenFromPlayer");
			if (methodInfo2 != null)
			{
				harmony.Patch(methodInfo2, null, null, null, new HarmonyMethod(typeof(Patch_PerformanceOptimizerRace), "FinalizeHidden"));
			}
		}
	}

	private static Exception FinalizeAlpha(Exception __exception, ref float __result)
	{
		if (!(__exception is InvalidOperationException))
		{
			return __exception;
		}
		__result = 1f;
		return null;
	}

	private static Exception FinalizeHidden(Exception __exception, ref bool __result)
	{
		if (!(__exception is InvalidOperationException))
		{
			return __exception;
		}
		__result = false;
		return null;
	}
}
