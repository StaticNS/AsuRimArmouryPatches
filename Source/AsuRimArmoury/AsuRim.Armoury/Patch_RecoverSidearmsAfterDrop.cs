using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn), "DropAndForbidEverything")]
internal static class Patch_RecoverSidearmsAfterDrop
{
	private static void Prefix(Pawn __instance, out List<ThingWithComps> __state)
	{
		__state = null;
		if (!KitRecovery.Tracks(__instance) || !__instance.Downed)
		{
			return;
		}
		CompArsenal compArsenal = CompArsenal.For(__instance);
		ThingOwner<Thing> thingOwner = __instance.inventory?.innerContainer;
		if (compArsenal == null || thingOwner == null)
		{
			return;
		}
		for (int i = 0; i < thingOwner.Count; i++)
		{
			if (thingOwner[i] is ThingWithComps thingWithComps && ArsenalRules.IsWieldable(thingWithComps) && KitRecovery.IsKit(compArsenal, thingWithComps))
			{
				if (__state == null)
				{
					__state = new List<ThingWithComps>();
				}
				__state.Add(thingWithComps);
			}
		}
	}

	private static void Postfix(Pawn __instance, List<ThingWithComps> __state)
	{
		if (__state == null)
		{
			return;
		}
		CompArsenal compArsenal = CompArsenal.For(__instance);
		if (compArsenal == null)
		{
			return;
		}
		for (int i = 0; i < __state.Count; i++)
		{
			ThingWithComps thingWithComps = __state[i];
			if (thingWithComps.Spawned && thingWithComps.Map == __instance.MapHeld)
			{
				KitRecovery.NoteSidearmDropped(compArsenal, thingWithComps);
			}
		}
	}
}
