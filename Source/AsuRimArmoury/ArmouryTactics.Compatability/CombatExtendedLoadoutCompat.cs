using System;
using System.Reflection;
using AsuRim.Armoury;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace ArmouryTactics.Compatability;

[StaticConstructorOnStartup]
internal static class CombatExtendedLoadoutCompat
{
	private static readonly Harmony harmony;

	static CombatExtendedLoadoutCompat()
	{
		harmony = new Harmony("asunib.RulesOfEngagement.CECompat");
		if (CombatExtendedCompat.Active)
		{
			PatchLoadoutJobGiver();
			PatchCreateLoadoutButton();
		}
	}

	private static void PatchLoadoutJobGiver()
	{
		Type type = AccessTools.TypeByName("CombatExtended.JobGiver_UpdateLoadout");
		if (!(type == null))
		{
			MethodInfo methodInfo = AccessTools.Method(type, "GetUpdateLoadoutJob");
			if (!(methodInfo == null))
			{
				harmony.Patch(methodInfo, null, new HarmonyMethod(typeof(CombatExtendedLoadoutCompat), "FilterWeaponJob"));
			}
		}
	}

	private static void FilterWeaponJob(Pawn pawn, ref Job __result)
	{
		if (__result != null && Owned(pawn))
		{
			__result = null;
		}
	}

	private static void PatchCreateLoadoutButton()
	{
		Type type = AccessTools.TypeByName("CombatExtended.ITab_Inventory");
		if (!(type == null))
		{
			MethodInfo methodInfo = AccessTools.Method(type, "SyncedAddLoadout");
			if (!(methodInfo == null))
			{
				harmony.Patch(methodInfo, new HarmonyMethod(typeof(CombatExtendedLoadoutCompat), "RedirectCreateLoadout"));
			}
		}
	}

	private static bool Owned(Pawn pawn)
	{
		if (!Arsenal.CanCarrySidearms(pawn))
		{
			return false;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null)
		{
			if (compArsenal.policy == null)
			{
				return compArsenal.stowed.Count > 0;
			}
			return true;
		}
		return false;
	}

	private static bool RedirectCreateLoadout(Pawn p)
	{
		if (!Arsenal.CanCarrySidearms(p))
		{
			return true;
		}
		CompArsenal compArsenal = CompArsenal.For(p);
		WeaponPolicyDatabase current = WeaponPolicyDatabase.Current;
		if (compArsenal == null || current == null)
		{
			return false;
		}
		WeaponPolicy policy = (compArsenal.policy = current.MakeFrom(p));
		Find.WindowStack.Add(new Dialog_EditLoadout(policy));
		return false;
	}
}
