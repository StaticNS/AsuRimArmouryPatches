using System;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Verb), "TryStartCastOn", new Type[]
{
	typeof(LocalTargetInfo),
	typeof(LocalTargetInfo),
	typeof(bool),
	typeof(bool),
	typeof(bool),
	typeof(bool)
})]
internal static class Patch_TryStartCastOn_Brawl
{
	private static bool Prefix(Verb __instance, ref bool __result)
	{
		if (!BrawlMeleeOnly.Blocks(__instance))
		{
			return true;
		}
		__result = false;
		return false;
	}
}
