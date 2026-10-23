using System;
using HarmonyLib;
using RimWorld;
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
internal static class Patch_TryStartCastOn_OffHand
{
	private static void Postfix(bool __result, Verb __instance, LocalTargetInfo castTarg)
	{
		Pawn pawn = __instance.caster as Pawn;
		if (!ArmouryTracked.OffHand.Contains(pawn) || !OffHandFiring.NativeActive || pawn.InMentalState || OffHandFiring.Active(pawn) || pawn.WorkTagIsDisabled(WorkTags.Violent) || __instance.IsMeleeAttack)
		{
			return;
		}
		ThingWithComps equipmentSource = __instance.EquipmentSource;
		if (equipmentSource == null || equipmentSource != Arsenal.MainHand(pawn))
		{
			return;
		}
		Thing thing = castTarg.Thing;
		if (thing == null || thing is Fire || (thing.Faction != null && thing.Faction == pawn.Faction))
		{
			return;
		}
		ThingWithComps thingWithComps = Arsenal.OffHand(pawn);
		if (thingWithComps == null || thingWithComps.def.IsMeleeWeapon)
		{
			return;
		}
		Pawn_StanceTracker pawn_StanceTracker = DualWield.OffHandStances(pawn);
		if (pawn_StanceTracker == null || pawn_StanceTracker.curStance is Stance_Busy)
		{
			return;
		}
		Verb verb = thingWithComps.GetComp<CompEquippable>()?.PrimaryVerb;
		if (verb == null || !verb.Available() || verb.verbProps.onlyManualCast)
		{
			return;
		}
		OffHandFiring.Begin(pawn);
		try
		{
			verb.TryStartCastOn(castTarg);
		}
		finally
		{
			OffHandFiring.End();
		}
	}
}
