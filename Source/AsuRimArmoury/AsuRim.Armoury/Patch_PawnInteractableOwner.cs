using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(ThingOwnerUtility), "TryGetInnerInteractableThingOwner")]
internal static class Patch_PawnInteractableOwner
{
	private static void Postfix(Thing thing, ref ThingOwner __result)
	{
		if (__result != null && __result.Owner is CompArsenal && thing is Pawn pawn)
		{
			__result = OwnerPastArsenal(pawn);
		}
	}

	internal static ThingOwner OwnerPastArsenal(Pawn pawn)
	{
		List<ThingComp> allComps = pawn.AllComps;
		for (int i = 0; i < allComps.Count; i++)
		{
			if (!(allComps[i] is CompArsenal) && allComps[i] is IThingHolder thingHolder)
			{
				ThingOwner directlyHeldThings = thingHolder.GetDirectlyHeldThings();
				if (directlyHeldThings != null)
				{
					return directlyHeldThings;
				}
			}
		}
		return pawn.inventory?.innerContainer;
	}
}
