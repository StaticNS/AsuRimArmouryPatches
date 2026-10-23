using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsuRim.Armoury.Patches;

/// <summary>
/// Our brand new buttons! Now gunpowder, alcohol, and luciferium flavour!
/// Patch for adding tabs for grenades and medicine to the policy editor.
/// </summary>
internal sealed class PolicyLoadoutExtension {
	private static readonly ConditionalWeakTable<WeaponPolicy, PolicyLoadoutExtension> Extensions =
		new ConditionalWeakTable<WeaponPolicy, PolicyLoadoutExtension>();

	internal List<LoadoutSlot> Grenades = new List<LoadoutSlot>();
	internal List<LoadoutSlot> Medicine = new List<LoadoutSlot>();

	/// <summary>
	/// I don't really know what this does, tbh
	/// </summary>
	/// <param name="policy"></param>
	/// <returns></returns>
	internal static PolicyLoadoutExtension For(WeaponPolicy policy) {
		PolicyLoadoutExtension extension = Extensions.GetValue(policy, _ => new PolicyLoadoutExtension());
		extension.SyncNativeFields(policy);
		return extension;
	}

	/// <summary>
	/// Copy function. For copying things.
	/// </summary>
	/// <param name="source"></param>
	internal void CopyFrom(PolicyLoadoutExtension source) {
		Grenades = CopySlots(source.Grenades);
		Medicine = CopySlots(source.Medicine);
	}

	/// <summary>
	/// Checking if the field might already exist, I guess?
	/// In case of future updates? Idk
	/// </summary>
	internal static bool HasNativeFields =>
		AccessTools.Field(typeof(WeaponPolicy), "grenades") != null
		&& AccessTools.Field(typeof(WeaponPolicy), "medicine") != null;

	/// <summary>
	/// If the field is already there, then sync them! 
	/// Our glorious lists will become their henious lists! 
	/// Or is it the other way around?
	/// </summary>
	/// <param name="policy"></param>
	private void SyncNativeFields(WeaponPolicy policy) {
		FieldInfo grenadesField = AccessTools.Field(typeof(WeaponPolicy), "grenades");
		FieldInfo medicineField = AccessTools.Field(typeof(WeaponPolicy), "medicine");
		if (grenadesField != null) {
			Grenades = (List<LoadoutSlot>)grenadesField.GetValue(policy) ?? Grenades;
			grenadesField.SetValue(policy, Grenades);
		}
		if (medicineField != null) {
			Medicine = (List<LoadoutSlot>)medicineField.GetValue(policy) ?? Medicine;
			medicineField.SetValue(policy, Medicine);
		}
	}

	/// <summary>
	/// Copy over the actual selected items and their targer counts.
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	private static List<LoadoutSlot> CopySlots(List<LoadoutSlot> source) {
		List<LoadoutSlot> copy = new List<LoadoutSlot>();
		if (source == null) {
			return copy;
		}

		for (int i = 0; i < source.Count; i++) {
			LoadoutSlot slot = source[i];
			if (slot != null) {
				copy.Add(new LoadoutSlot {
					def = slot.def,
					stuff = slot.stuff,
					count = slot.count,
					minQuality = slot.minQuality
				});
			}
		}

		return copy;
	}
}

[HarmonyPatch(typeof(WeaponPolicy), nameof(WeaponPolicy.ExposeData))]
internal static class Patch_WeaponPolicy_ExposeData {
	private static void Postfix(WeaponPolicy __instance) {
		if (PolicyLoadoutExtension.HasNativeFields) {
			return;
		}
		PolicyLoadoutExtension extension = PolicyLoadoutExtension.For(__instance);
		Scribe_Collections.Look(ref extension.Grenades, "grenades", LookMode.Deep);
		Scribe_Collections.Look(ref extension.Medicine, "medicine", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.PostLoadInit) {
			extension.Grenades ??= new List<LoadoutSlot>();
			extension.Medicine ??= new List<LoadoutSlot>();
			extension.Grenades.RemoveAll(slot => slot == null || slot.def == null);
			extension.Medicine.RemoveAll(slot => slot == null || slot.def == null);
		}
	}
}

[HarmonyPatch(typeof(WeaponPolicy), nameof(WeaponPolicy.CopyFrom))]
internal static class Patch_WeaponPolicy_CopyFrom {
	private static void Postfix(WeaponPolicy __instance, Policy other) {
		if (other is WeaponPolicy source) {
			PolicyLoadoutExtension extension = PolicyLoadoutExtension.For(__instance);
			extension.CopyFrom(PolicyLoadoutExtension.For(source));
			PolicyLoadoutExtension.For(__instance);
		}
	}
}
