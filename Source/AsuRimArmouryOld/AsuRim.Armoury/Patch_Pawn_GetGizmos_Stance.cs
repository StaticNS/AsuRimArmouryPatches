using System.Collections.Generic;
using ArmouryTactics.Compatability;
using HarmonyLib;
using Verse;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn), "GetGizmos")]
internal static class Patch_Pawn_GetGizmos_Stance
{
	private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
	{
		if (CompArsenal.For(__instance) == null)
		{
			return values;
		}
		return Appended(values, __instance);
	}

	private static IEnumerable<Gizmo> Appended(IEnumerable<Gizmo> values, Pawn __instance)
	{
		IEnumerable<Gizmo> enumerable = (ArmourySettings.dualWieldEnabled ? DualWieldAttackGizmo.Process(values, __instance) : values);
		foreach (Gizmo item in enumerable)
		{
			yield return item;
		}
		if (ArmourySettings.dualWieldEnabled && CombatExtendedDualWieldAmmoCompat.Available)
		{
			ThingWithComps thingWithComps = Arsenal.OffHand(__instance);
			if (thingWithComps != null)
			{
				foreach (Gizmo item2 in CombatExtendedDualWieldAmmoCompat.OffHandAmmoGizmos(thingWithComps))
				{
					yield return item2;
				}
			}
		}
		foreach (Gizmo item3 in FormationGizmo.For(__instance))
		{
			yield return item3;
		}
		foreach (Gizmo item4 in AnimalGuardGizmo.For(__instance))
		{
			yield return item4;
		}
		CompArsenal compArsenal = CompArsenal.For(__instance);
		bool stances = ArmourySettings.stancesEnabled;
		bool sidearms = ArmourySettings.sidearmSystemEnabled;
		bool panelShown = sidearms && ArmourySettings.nativeLoadoutUIEnabled && ArmourySettings.loadoutPanel
			&& compArsenal != null && Arsenal.CanCarrySidearms(__instance) && StanceRules.CanTakeOrders(__instance);
		if (!panelShown && sidearms && Arsenal.CanCarrySidearms(__instance) && StanceRules.CanTakeOrders(__instance))
		{
			foreach (Gizmo item in ReArmGizmo.For(__instance))
			{
				yield return item;
			}
		}
		if (sidearms && ArmourySettings.nativeLoadoutUIEnabled && ArmourySettings.loadoutPanel && compArsenal != null && Arsenal.CanCarrySidearms(__instance) && StanceRules.CanTakeOrders(__instance))
		{
			yield return Gizmo_LoadoutPanel.For(__instance, compArsenal);
		}
		else
		{
			if (!StanceRules.ShowsCombatControls(__instance))
			{
				if (sidearms && Arsenal.CanCarrySidearms(__instance) && StanceRules.CanTakeOrders(__instance))
				{
					if (ArmourySettings.nativeLoadoutUIEnabled && !ArmourySettings.loadoutPanel)
					{
						foreach (Gizmo item in LoadoutPanelToggleGizmo.For(__instance))
						{
							yield return item;
						}
					}
				}
				yield break;
			}
			if (stances)
			{
				foreach (Gizmo item5 in StanceGizmo.For(__instance))
				{
					yield return item5;
				}
			}
			if (sidearms)
			{
				foreach (Gizmo item6 in LoadoutGizmo.For(__instance))
				{
					yield return item6;
				}
			}
			if (stances)
			{
				foreach (Gizmo item7 in OverwatchGizmo.For(__instance))
				{
					yield return item7;
				}
			}
			if (sidearms && ArmourySettings.nativeLoadoutUIEnabled && !ArmourySettings.loadoutPanel && Arsenal.CanCarrySidearms(__instance))
			{
				foreach (Gizmo item8 in LoadoutPanelToggleGizmo.For(__instance))
				{
					yield return item8;
				}
			}
			foreach (Gizmo item9 in WalkFireGizmo.For(__instance))
			{
				yield return item9;
			}
		}
	}
}
