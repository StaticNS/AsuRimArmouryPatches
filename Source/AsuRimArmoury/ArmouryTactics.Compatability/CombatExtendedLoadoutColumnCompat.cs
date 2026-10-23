using System;
using System.Reflection;
using AsuRim.Armoury;
using HarmonyLib;
using Verse;

namespace ArmouryTactics.Compatability;

[StaticConstructorOnStartup]
internal static class CombatExtendedLoadoutColumnCompat
{
	private static readonly Harmony harmony;

	internal static bool Suppress
	{
		get
		{
			if (CombatExtendedCompat.Active && !Arsenal.SidearmsStoodDownForCE)
			{
				return ArmourySettings.sidearmSystemEnabled;
			}
			return false;
		}
	}

	static CombatExtendedLoadoutColumnCompat()
	{
		harmony = new Harmony("asunib.RulesOfEngagement.CECompat.Column");
		if (CombatExtendedCompat.Active)
		{
			Type type = AccessTools.TypeByName("CombatExtended.PawnColumnWorker_Loadout");
			if (!(type == null))
			{
				PatchSkip(type, "DoHeader");
				PatchSkip(type, "DoCell");
				PatchZeroWidth(type, "GetMinWidth");
				PatchZeroWidth(type, "GetOptimalWidth");
				PatchZeroWidth(type, "GetMinHeaderHeight");
			}
		}
	}

	private static void PatchSkip(Type columnType, string methodName)
	{
		MethodInfo methodInfo = AccessTools.Method(columnType, methodName);
		if (!(methodInfo == null))
		{
			harmony.Patch(methodInfo, new HarmonyMethod(typeof(CombatExtendedLoadoutColumnCompat), "SkipIfSuppressed"));
		}
	}

	private static void PatchZeroWidth(Type columnType, string methodName)
	{
		MethodInfo methodInfo = AccessTools.Method(columnType, methodName);
		if (!(methodInfo == null))
		{
			harmony.Patch(methodInfo, new HarmonyMethod(typeof(CombatExtendedLoadoutColumnCompat), "ZeroIfSuppressed"));
		}
	}

	private static bool SkipIfSuppressed()
	{
		return !Suppress;
	}

	private static bool ZeroIfSuppressed(ref int __result)
	{
		if (!Suppress)
		{
			return true;
		}
		__result = 0;
		return false;
	}
}
