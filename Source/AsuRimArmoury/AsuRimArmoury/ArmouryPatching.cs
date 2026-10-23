using System;
using AsuRim.Armoury;
using AsuRimArmoury.Kit;
using HarmonyLib;

namespace AsuRimArmoury;

internal static class ArmouryPatching
{
	private const int SettingsOwners = 2;

	private static int settingsLoaded;

	private static bool patched;

	internal static void SettingsLoaded()
	{
		settingsLoaded++;
		if (settingsLoaded >= 2)
		{
			EnsurePatched();
		}
	}

	internal static bool Include(Type type)
	{
		if (GuardStandDown.Active)
		{
			return !type.IsDefined(typeof(GuardingPatchAttribute), inherit: false);
		}
		return true;
	}

	internal static void EnsurePatched()
	{
		if (!patched)
		{
			patched = true;
			PatchGuard.PatchAll(new Harmony("asunib.RulesOfEngagement"), typeof(ArmouryPatching).Assembly, "RulesOfEngagement", Include);
		}
	}
}
