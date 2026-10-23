using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn), "Strip")]
internal static class Patch_StripOffHand
{
	private static void Prefix(Pawn __instance, out bool __state)
	{
		KitRecovery.deliberate++;
		__state = true;
		CompArsenal compArsenal = CompArsenal.For(__instance);
		if (compArsenal?.offHand != null)
		{
			ThingWithComps thingWithComps = compArsenal.TakeOffHand();
			if (__instance.inventory == null || !__instance.inventory.innerContainer.TryAdd(thingWithComps))
			{
				compArsenal.SetOffHand(thingWithComps);
				ArsenalActions.ReleaseOffHand(__instance);
			}
		}
	}

	private static void Finalizer(bool __state)
	{
		if (__state)
		{
			KitRecovery.deliberate--;
		}
	}
}
