using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Caravan_TraderTracker), "ColonyThingsWillingToBuy")]
internal static class Patch_CaravanWillingToBuy
{
	private static IEnumerable<Thing> Postfix(IEnumerable<Thing> values, Pawn playerNegotiator)
	{
		return TradeFilter.Without(values, playerNegotiator);
	}
}
