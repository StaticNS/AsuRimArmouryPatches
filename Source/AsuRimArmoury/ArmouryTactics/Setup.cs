using System;
using System.Collections.Generic;
using System.Reflection;
using AsuRimArmoury;
using HarmonyLib;
using Verse;

namespace ArmouryTactics;

[StaticConstructorOnStartup]
public static class Setup
{
	public delegate ThingWithComps OffHandShield(Pawn pawn);

	public delegate bool IsShield(ThingDef tDef);

	public static ThingDef[] allWeapons;

	static Setup()
	{
		SetupModCompatability();
		ArmouryPatching.EnsurePatched();
		SetupRnG();
		SetupDW();
		SetupDWCustomRotations();
	}

	private static void SetupRnG()
	{
		ModSettings_ArmouryTactics.heavyWeaponsCache = new HashSet<ushort>();
		ModSettings_ArmouryTactics.forbiddenWeaponsCache = new HashSet<ushort>();
		if (ModSettings_ArmouryTactics.heavyWeapons == null)
		{
			ModSettings_ArmouryTactics.heavyWeapons = new List<string>();
		}
		if (ModSettings_ArmouryTactics.forbiddenWeapons == null)
		{
			ModSettings_ArmouryTactics.forbiddenWeapons = new List<string>();
		}
		List<ThingDef> list = new List<ThingDef>();
		int defCount = DefDatabase<ThingDef>.DefCount;
		while (defCount-- > 0)
		{
			ThingDef thingDef = DefDatabase<ThingDef>.defsList[defCount];
			if (thingDef.equipmentType == EquipmentType.Primary && !thingDef.weaponTags.NullOrEmpty() && !thingDef.destroyOnDrop)
			{
				list.Add(thingDef);
				bool flag = thingDef.BaseMass > 3.4f;
				if (ModSettings_ArmouryTactics.heavyWeapons.Contains(thingDef.defName))
				{
					flag = !flag;
				}
				if (flag)
				{
					ModSettings_ArmouryTactics.heavyWeaponsCache.Add(thingDef.shortHash);
				}
				flag = thingDef.HasModExtension<WeaponForbidden>();
				if (ModSettings_ArmouryTactics.forbiddenWeapons.Contains(thingDef.defName))
				{
					flag = !flag;
				}
				if (flag)
				{
					ModSettings_ArmouryTactics.forbiddenWeaponsCache.Add(thingDef.shortHash);
				}
			}
		}
		list.SortBy((ThingDef x) => x.label);
		allWeapons = list.ToArray();
	}

	public static void SetupDW(bool processInversions = false)
	{
		HashSet<ushort> hashSet;
		HashSet<ushort> hashSet2;
		if (processInversions)
		{
			hashSet = new HashSet<ushort>(ModSettings_ArmouryTactics.offHandersCache);
			hashSet2 = new HashSet<ushort>(ModSettings_ArmouryTactics.twoHandersCache);
		}
		else
		{
			hashSet = (hashSet2 = null);
		}
		ModSettings_ArmouryTactics.offHandersCache = new HashSet<ushort>();
		ModSettings_ArmouryTactics.twoHandersCache = new HashSet<ushort>();
		if (ModSettings_ArmouryTactics.offHanders == null)
		{
			ModSettings_ArmouryTactics.offHanders = new List<string>();
		}
		if (ModSettings_ArmouryTactics.twoHanders == null)
		{
			ModSettings_ArmouryTactics.twoHanders = new List<string>();
		}
		int num = allWeapons.Length;
		while (num-- > 0)
		{
			ThingDef thingDef = allWeapons[num];
			ushort shortHash = thingDef.shortHash;
			List<DefModExtension> modExtensions = thingDef.modExtensions;
			if (modExtensions != null)
			{
				int count = modExtensions.Count;
				while (count-- > 0)
				{
					DefModExtension defModExtension = modExtensions[count];
					if (defModExtension is OffHander)
					{
						ModSettings_ArmouryTactics.offHandersCache.Add(shortHash);
					}
					else if (defModExtension is TwoHander || defModExtension is WeaponForbidden)
					{
						ModSettings_ArmouryTactics.twoHandersCache.Add(shortHash);
					}
				}
			}
			if (!ModSettings_ArmouryTactics.heavyWeaponsCache.Contains(shortHash) && !ModSettings_ArmouryTactics.twoHandersCache.Contains(shortHash))
			{
				ModSettings_ArmouryTactics.offHandersCache.Add(shortHash);
			}
			else
			{
				ModSettings_ArmouryTactics.twoHandersCache.Add(shortHash);
			}
			if (processInversions)
			{
				if (hashSet.Contains(shortHash) != ModSettings_ArmouryTactics.offHandersCache.Contains(shortHash))
				{
					ModSettings_ArmouryTactics.offHanders.Add(thingDef.defName);
				}
				else if (ModSettings_ArmouryTactics.offHanders.Contains(thingDef.defName))
				{
					ModSettings_ArmouryTactics.offHanders.Remove(thingDef.defName);
				}
				if (hashSet2.Contains(shortHash) != ModSettings_ArmouryTactics.twoHandersCache.Contains(shortHash))
				{
					ModSettings_ArmouryTactics.twoHanders.Add(thingDef.defName);
				}
				else if (ModSettings_ArmouryTactics.twoHanders.Contains(thingDef.defName))
				{
					ModSettings_ArmouryTactics.twoHanders.Remove(thingDef.defName);
				}
				continue;
			}
			if (ModSettings_ArmouryTactics.offHanders.Contains(thingDef.defName))
			{
				if (ModSettings_ArmouryTactics.offHandersCache.Contains(shortHash))
				{
					ModSettings_ArmouryTactics.offHandersCache.Remove(shortHash);
				}
				else
				{
					ModSettings_ArmouryTactics.offHandersCache.Add(shortHash);
				}
			}
			if (ModSettings_ArmouryTactics.twoHanders.Contains(thingDef.defName))
			{
				if (ModSettings_ArmouryTactics.twoHandersCache.Contains(shortHash))
				{
					ModSettings_ArmouryTactics.twoHandersCache.Remove(shortHash);
				}
				else
				{
					ModSettings_ArmouryTactics.twoHandersCache.Add(shortHash);
				}
			}
		}
		if (processInversions)
		{
			ModSettings_ArmouryTactics.offHandersCache = hashSet;
			ModSettings_ArmouryTactics.twoHandersCache = hashSet2;
		}
	}

	private static void SetupDWCustomRotations()
	{
		ModSettings_ArmouryTactics.customRotationsCache = new Dictionary<ushort, int>();
		if (ModSettings_ArmouryTactics.customRotations == null)
		{
			ModSettings_ArmouryTactics.customRotations = new Dictionary<string, int>();
		}
		int num = allWeapons.Length;
		while (num-- > 0)
		{
			ThingDef thingDef = allWeapons[num];
			CustomRotation modExtension = thingDef.GetModExtension<CustomRotation>();
			int num2 = 0;
			if (modExtension != null)
			{
				num2 = modExtension.extraRotation;
			}
			ModSettings_ArmouryTactics.customRotations.TryGetValue(thingDef.defName, out var value);
			int num3 = num2 + value;
			if (num3 != 0)
			{
				ModSettings_ArmouryTactics.customRotationsCache.Add(thingDef.shortHash, num3);
			}
		}
	}

	public static void CheckInvesions()
	{
		ModSettings_ArmouryTactics.heavyWeapons = new List<string>();
		ModSettings_ArmouryTactics.forbiddenWeapons = new List<string>();
		int num = allWeapons.Length;
		while (num-- > 0)
		{
			ThingDef thingDef = allWeapons[num];
			if (thingDef.BaseMass > 3.4f)
			{
				if (!ModSettings_ArmouryTactics.heavyWeaponsCache.Contains(thingDef.shortHash))
				{
					ModSettings_ArmouryTactics.heavyWeapons.Add(thingDef.defName);
				}
			}
			else if (ModSettings_ArmouryTactics.heavyWeaponsCache.Contains(thingDef.shortHash))
			{
				ModSettings_ArmouryTactics.heavyWeapons.Add(thingDef.defName);
			}
			if (thingDef.HasModExtension<WeaponForbidden>())
			{
				if (!ModSettings_ArmouryTactics.forbiddenWeaponsCache.Contains(thingDef.shortHash))
				{
					ModSettings_ArmouryTactics.forbiddenWeapons.Add(thingDef.defName);
				}
			}
			else if (ModSettings_ArmouryTactics.forbiddenWeaponsCache.Contains(thingDef.shortHash))
			{
				ModSettings_ArmouryTactics.forbiddenWeapons.Add(thingDef.defName);
			}
		}
	}

	private static void SetupModCompatability()
	{
		Type type = AccessTools.TypeByName("VFECore.ShieldUtility") ?? AccessTools.TypeByName("VEF.Apparels.ShieldUtility");
		MethodInfo methodInfo = type?.GetMethod("OffHandShield");
		if (methodInfo != null)
		{
			ModSettings_ArmouryTactics.OffHandShield = AccessTools.MethodDelegate<OffHandShield>(methodInfo);
			ModSettings_ArmouryTactics.VFECoreEnabled = true;
		}
		methodInfo = type?.GetMethod("IsShield", new Type[1] { typeof(ThingDef) });
		if (methodInfo != null)
		{
			ModSettings_ArmouryTactics.IsShield = AccessTools.MethodDelegate<IsShield>(methodInfo);
		}
	}
}
