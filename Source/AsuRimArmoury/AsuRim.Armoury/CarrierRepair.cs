using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

internal static class CarrierRepair
{
	internal static void Run(Pawn pawn)
	{
		if (pawn == null || !pawn.Spawned)
		{
			return;
		}
		CompMechCarrier comp = pawn.GetComp<CompMechCarrier>();
		ThingDef thingDef = comp?.Props?.fixedIngredient;
		ThingOwner thingOwner = pawn.inventory?.innerContainer;
		ThingOwner thingOwner2 = comp?.GetDirectlyHeldThings();
		if (thingDef == null || thingOwner == null || thingOwner2 == null)
		{
			return;
		}
		for (int num = thingOwner.Count - 1; num >= 0; num--)
		{
			if (num < thingOwner.Count)
			{
				Thing thing = thingOwner[num];
				if (thing.def == thingDef)
				{
					int num2 = Mathf.Max(0, comp.Props.maxIngredientCount - comp.IngredientCount);
					if (num2 > 0)
					{
						thingOwner.TryTransferToContainer(thing, thingOwner2, Mathf.Min(num2, thing.stackCount));
					}
					if (!thing.Destroyed && thing.holdingOwner == thingOwner && thing.stackCount > 0)
					{
						thingOwner.TryDrop(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, out var _);
					}
				}
			}
		}
	}
}
