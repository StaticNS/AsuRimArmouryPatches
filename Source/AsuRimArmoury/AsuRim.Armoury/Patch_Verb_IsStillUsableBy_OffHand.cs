using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Verb), "IsStillUsableBy")]
internal static class Patch_Verb_IsStillUsableBy_OffHand
{
	private static bool Prefix(Verb __instance, Pawn pawn, ref bool __result)
	{
		ThingWithComps equipmentSource = __instance.EquipmentSource;
		if (equipmentSource == null || !Arsenal.IsOffHand(equipmentSource))
		{
			return true;
		}
		if (Arsenal.OffHand(pawn) != equipmentSource || !__instance.Available() || __instance.verbProps.GetDamageFactorFor(__instance, pawn) == 0f || (pawn.IsSubhuman && __instance.verbProps.category == VerbCategory.Ignite))
		{
			__result = false;
			return false;
		}
		__result = true;
		return false;
	}
}
