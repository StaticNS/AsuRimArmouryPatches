using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
internal static class VEFShieldCompat
{
	internal delegate ThingWithComps OffHandShieldDelegate(Pawn pawn);

	internal delegate bool IsShieldDefDelegate(ThingDef def);

	internal static OffHandShieldDelegate OffHandShield;

	private static IsShieldDefDelegate isShieldDef;

	internal static bool Active => OffHandShield != null;

	static VEFShieldCompat()
	{
		Type type = AccessTools.TypeByName("VFECore.ShieldUtility") ?? AccessTools.TypeByName("VEF.Apparels.ShieldUtility");
		if (!(type == null))
		{
			MethodInfo methodInfo = AccessTools.Method(type, "OffHandShield");
			if (methodInfo != null)
			{
				OffHandShield = AccessTools.MethodDelegate<OffHandShieldDelegate>(methodInfo);
			}
			Harmony harmony = new Harmony("asunib.RulesOfEngagement.VEFShieldCompat");
			MethodInfo methodInfo2 = AccessTools.Method(type, "IsShield", new Type[1] { typeof(ThingDef) });
			if (methodInfo2 != null)
			{
				isShieldDef = AccessTools.MethodDelegate<IsShieldDefDelegate>(methodInfo2);
				harmony.Patch(AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear"), null, new HarmonyMethod(typeof(VEFShieldCompat), "PostfixWear"));
			}
			MethodInfo methodInfo3 = AccessTools.Method(type, "UsableWithShields");
			if (methodInfo3 != null)
			{
				harmony.Patch(methodInfo3, new HarmonyMethod(typeof(VEFShieldCompat), "PrefixUsableWithShields"));
			}
		}
	}

	private static bool PrefixUsableWithShields(ThingDef def, ref bool __result)
	{
		if (def == null || (!def.IsRangedWeapon && !def.IsMeleeWeapon))
		{
			return true;
		}
		if (!ArsenalRules.TryGetExplicitHands(def, out var twoHanded))
		{
			return true;
		}
		__result = !twoHanded;
		return false;
	}

	private static void PostfixWear(Pawn_ApparelTracker __instance, Apparel newApparel)
	{
		if (isShieldDef != null && newApparel != null && isShieldDef(newApparel.def))
		{
			Pawn pawn = __instance.pawn;
			if (Arsenal.OffHand(pawn) != null)
			{
				ArsenalActions.StowOffHand(pawn);
			}
		}
	}
}
