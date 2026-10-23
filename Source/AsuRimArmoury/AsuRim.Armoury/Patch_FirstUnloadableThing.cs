using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_InventoryTracker), "FirstUnloadableThing", MethodType.Getter)]
internal static class Patch_FirstUnloadableThing
{
	private static void Postfix(Pawn_InventoryTracker __instance, ref ThingCount __result)
	{
		if (__result == default(ThingCount))
		{
			return;
		}
		Pawn pawn = __instance.pawn;
		if (!SidearmSafety.Protected(pawn, __result.Thing))
		{
			return;
		}
		ThingOwner<Thing> innerContainer = __instance.innerContainer;
		for (int i = 0; i < innerContainer.Count; i++)
		{
			Thing thing = innerContainer[i];
			if (!SidearmSafety.Protected(pawn, thing) && !Kept(pawn, thing))
			{
				__result = new ThingCount(thing, thing.stackCount);
				return;
			}
		}
		__result = default;
	}

	private static bool Kept(Pawn pawn, Thing thing)
	{
		DrugPolicy drugPolicy = pawn.drugs?.CurrentPolicy;
		if (drugPolicy != null)
		{
			for (int i = 0; i < drugPolicy.Count; i++)
			{
				if (drugPolicy[i].drug == thing.def && drugPolicy[i].takeToInventory > 0)
				{
					return true;
				}
			}
		}
		Pawn_InventoryStockTracker inventoryStock = pawn.inventoryStock;
		if (inventoryStock?.stockEntries != null)
		{
			foreach (InventoryStockEntry value in inventoryStock.stockEntries.Values)
			{
				if (value.thingDef == thing.def)
				{
					return true;
				}
			}
		}
		if (pawn.IsColonist && thing.def.IsNutritionGivingIngestible && !thing.def.IsDrug)
		{
			return JobGiver_PackFood.IsGoodPackableFoodFor(thing, pawn, checkMass: false);
		}
		return false;
	}
}
