using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

public static class LoadoutGizmo {
	public static IEnumerable<Gizmo> For(Pawn pawn) {
		CompArsenal comp = CompArsenal.For(pawn);
		if (comp != null && StanceRules.CanTakeOrders(pawn) && Arsenal.CanCarrySidearms(pawn)) {
			yield return new Command_Action {
				defaultLabel = (comp.policy != null) ? comp.policy.label : ArmouryText.Get("ASA_Policy_None"),
				defaultDesc = Description(comp.policy),
				icon = StanceIcons.Get("UI/Stances/ASA_Loadout", TexCommand.ForbidOff),
				groupKey = 84610002,
				action = () => {
					Find.WindowStack.Add(new FloatMenu(AllOptions(pawn, comp)));
				}
			};
		}
	}

	private static string Description(WeaponPolicy policy) {
		if (policy == null) {
			return ArmouryText.Get("ASA_Policy_NoneDesc");
		}
		return policy.label + "\n\n" + policy.Summary;
	}

	private static List<FloatMenuOption> AllOptions(Pawn pawn, CompArsenal comp) {
		return MenuFor(pawn, comp);
	}

	internal static List<FloatMenuOption> MenuFor(Pawn pawn, CompArsenal comp) {
		List<FloatMenuOption> list = PolicyOptions(pawn, comp);
		list.AddRange(ItemOptions(pawn, comp));
		return list;
	}

	internal static List<FloatMenuOption> PolicyOptions(Pawn pawn, CompArsenal comp) {
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		list.Add(new FloatMenuOption("ASA_Policy_Assign".Translate(), () => {
			Find.WindowStack.Add(new FloatMenu(AssignOptions(pawn, comp)));
		}));
		list.Add(new FloatMenuOption("ASA_Policy_SaveCurrent".Translate(), () => {
			if (comp.policy != null) {
				comp.policy.CaptureFrom(pawn);
			} else {
				WeaponPolicyDatabase current = WeaponPolicyDatabase.Current;
				if (current != null) {
					comp.policy = current.MakeFrom(pawn);
				}
			}
		}));
		if (comp.policy != null) {
			WeaponPolicy editing = comp.policy;
			list.Add(new FloatMenuOption("ASA_Policy_Edit".Translate(editing.label), () => {
				Find.WindowStack.Add(new Dialog_EditLoadout(editing, pawn));
			}));
		}
		return list;
	}

	private static List<FloatMenuOption> AssignOptions(Pawn pawn, CompArsenal comp) {
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		WeaponPolicyDatabase db = WeaponPolicyDatabase.Current;
		if (db == null) {
			return list;
		}
		list.Add(new FloatMenuOption("ASA_Policy_None".Translate(), () => {
			List<Pawn> list2 = Commanded.With(pawn);
			for (int i = 0; i < list2.Count; i++) {
				CompArsenal compArsenal = CompArsenal.For(list2[i]);
				if (compArsenal != null) {
					compArsenal.policy = null;
				}
			}
		}));
		for (int num = 0; num < db.Policies.Count; num++) {
			WeaponPolicy policy = db.Policies[num];
			Texture2D iconTex = ((policy.mainDef != null) ? Widgets.GetIconFor(policy.mainDef, policy.mainStuff) : BaseContent.BadTex);
			list.Add(new FloatMenuOption(policy.label + " (" + policy.Summary + ")", () => {
				List<Pawn> list2 = Commanded.With(pawn);
				for (int i = 0; i < list2.Count; i++) {
					CompArsenal compArsenal = CompArsenal.For(list2[i]);
					if (compArsenal != null) {
						compArsenal.policy = policy;
						PolicyApplier.Apply(list2[i], policy);
					}
				}
			}, iconTex, Color.white));
		}
		list.Add(new FloatMenuOption("ASA_Policy_NewOne".Translate(), () => {
			WeaponPolicy policy2 = db.MakeNew();
			comp.policy = policy2;
			Find.WindowStack.Add(new Dialog_EditLoadout(policy2, pawn));
		}));
		return list;
	}

	private static List<FloatMenuOption> ItemOptions(Pawn pawn, CompArsenal comp) {
		List<ThingWithComps> list = new List<ThingWithComps>();
		Arsenal.CollectAvailable(pawn, list, includeHeld: false);
		ThingWithComps thingWithComps = Arsenal.OffHand(pawn);
		if (thingWithComps != null) {
			list.Insert(0, thingWithComps);
		}
		List<FloatMenuOption> list2 = new List<FloatMenuOption>();
		WeaponPolicy policy = comp?.policy;
		bool flag = ArmourySettings.dualWieldEnabled && policy != null && policy.IsPair;
		if (flag) {
			Texture2D iconFor = Widgets.GetIconFor(policy.mainDef, policy.mainStuff);
			list2.Add(new FloatMenuOption("ASA_Loadout_SwapTo".Translate(policy.Summary), () => {
				if (comp != null) {
					comp.forcedUnarmed = false;
				}
				PolicyApplier.Apply(pawn, policy);
			}, iconFor, Color.white));
		}
		for (int num = 0; num < list.Count; num++) {
			ThingWithComps weapon = list[num];
			if ((flag && InPair(policy, weapon)) || !ArsenalRules.CanWield(pawn, weapon)) {
				continue;
			}
			Texture2D iconFor2 = Widgets.GetIconFor(weapon.def, weapon.Stuff);
			list2.Add(new FloatMenuOption("ASA_Loadout_SwapTo".Translate(weapon.LabelCap), () => {
				if (comp != null) {
					comp.forcedUnarmed = false;
				}
				ArsenalActions.EquipMain(pawn, weapon, dropCurrent: false, manual: true);
			}, iconFor2, Color.white));
		}
		return list2;
	}

	private static bool InPair(WeaponPolicy policy, ThingWithComps weapon) {
		if (weapon.def == policy.mainDef && weapon.Stuff == policy.mainStuff) {
			return true;
		}
		if (weapon.def == policy.offDef) {
			return weapon.Stuff == policy.offStuff;
		}
		return false;
	}
}
