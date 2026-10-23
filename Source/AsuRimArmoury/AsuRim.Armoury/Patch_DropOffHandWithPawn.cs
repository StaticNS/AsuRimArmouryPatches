using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn), "DropAndForbidEverything")]
internal static class Patch_DropOffHandWithPawn
{
	private static void Prefix(Pawn __instance, bool keepInventoryAndEquipmentIfInBed)
	{
		CompArsenal compArsenal = CompArsenal.For(__instance);
		ThingWithComps thingWithComps = compArsenal?.offHand;
		if (thingWithComps == null)
		{
			return;
		}
		ThingWithComps thingWithComps2 = Arsenal.MainHand(__instance);
		if (thingWithComps2 != null)
		{
			ArsenalActions.RememberQuietly(__instance, thingWithComps2);
		}
		ArsenalActions.RememberQuietly(__instance, thingWithComps);
		if (__instance.kindDef.destroyGearOnDrop)
		{
			compArsenal.DestroyOffHand();
		}
		else if (__instance.InContainerEnclosed)
		{
			ThingWithComps thingWithComps3 = compArsenal.TakeOffHand();
			if (__instance.holdingOwner == null || !__instance.holdingOwner.TryAdd(thingWithComps3))
			{
				compArsenal.SetOffHand(thingWithComps3);
			}
		}
		else if (__instance.SpawnedOrAnyParentSpawned && (!keepInventoryAndEquipmentIfInBed || !__instance.InBed()))
		{
			ThingWithComps thingWithComps4 = compArsenal.TakeOffHand();
			if (!GenPlace.TryPlaceThing(thingWithComps4, __instance.PositionHeld, __instance.MapHeld, ThingPlaceMode.Near))
			{
				compArsenal.SetOffHand(thingWithComps4);
				return;
			}
			thingWithComps4.SetForbidden(value: true, warnOnFail: false);
			compArsenal.downOffThing = thingWithComps4;
		}
	}
}
