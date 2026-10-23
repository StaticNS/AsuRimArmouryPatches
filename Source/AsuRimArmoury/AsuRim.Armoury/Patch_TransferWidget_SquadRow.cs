using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace AsuRim.Armoury;

[HarmonyPatch]
internal static class Patch_TransferWidget_SquadRow
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(TransferableOneWayWidget), "OnGUI", new Type[2]
		{
			typeof(Rect),
			typeof(bool).MakeByRefType()
		});
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix(TransferableOneWayWidget __instance, Rect inRect, ref bool anythingChanged)
	{
		if (CaravanSquadRow.IsPawnsWidget(__instance))
		{
			CaravanSquadRow.Draw(inRect, ref anythingChanged);
		}
	}
}
