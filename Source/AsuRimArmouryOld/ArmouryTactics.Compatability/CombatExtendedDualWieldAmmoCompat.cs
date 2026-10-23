using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ArmouryTactics.Compatability;

internal static class CombatExtendedDualWieldAmmoCompat
{
	private static readonly Type compAmmoUserType = AccessTools.TypeByName("CombatExtended.CompAmmoUser");

	private static readonly MethodInfo compGetGizmosExtra = ((compAmmoUserType == null) ? null : AccessTools.Method(compAmmoUserType, "CompGetGizmosExtra"));

	internal static bool Available
	{
		get
		{
			if (CombatExtendedCompat.Active && compAmmoUserType != null)
			{
				return compGetGizmosExtra != null;
			}
			return false;
		}
	}

	internal static IEnumerable<Gizmo> OffHandAmmoGizmos(ThingWithComps weapon)
	{
		if (!Available || weapon == null)
		{
			return Enumerable.Empty<Gizmo>();
		}
		List<ThingComp> allComps = weapon.AllComps;
		for (int i = 0; i < allComps.Count; i++)
		{
			if (compAmmoUserType.IsInstanceOfType(allComps[i]))
			{
				return allComps[i].CompGetGizmosExtra() ?? Enumerable.Empty<Gizmo>();
			}
		}
		return Enumerable.Empty<Gizmo>();
	}
}
