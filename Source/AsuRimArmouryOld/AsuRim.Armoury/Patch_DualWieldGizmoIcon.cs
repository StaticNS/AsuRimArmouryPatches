using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch]
internal static class Patch_DualWieldGizmoIcon
{
	private static bool failed;

	private static MethodBase TargetMethod()
	{
		return AccessTools.DeclaredMethod(typeof(Command_VerbTarget), "DrawIcon");
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static bool Prefix(Command_VerbTarget __instance, Rect rect, GizmoRenderParms parms)
	{
		if (failed)
		{
			return true;
		}
		try
		{
			Verb verb = __instance.verb;
			ThingWithComps thingWithComps = verb?.EquipmentSource;
			if (thingWithComps == null || thingWithComps.Destroyed)
			{
				return true;
			}
			Pawn casterPawn = verb.CasterPawn;
			if (casterPawn == null)
			{
				return true;
			}
			if (Arsenal.MainHand(casterPawn) != thingWithComps)
			{
				return true;
			}
			ThingWithComps thingWithComps2 = Arsenal.OffHand(casterPawn);
			if (thingWithComps2 == null || thingWithComps2 == thingWithComps || thingWithComps2.Destroyed)
			{
				return true;
			}
			if (thingWithComps.def.IsMeleeWeapon != thingWithComps2.def.IsMeleeWeapon)
			{
				return true;
			}
			PairIcon.Draw(rect, thingWithComps, thingWithComps2, __instance.iconDrawScale * 0.85f, parms.lowLight);
			return false;
		}
		catch (Exception ex)
		{
			failed = true;
			Log.Error("[AsuRim Armoury] dual-wield gizmo icon failed, handing the button back to vanilla: " + ex);
			return true;
		}
	}
}
