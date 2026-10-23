using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

internal static class LoadoutEditorUI {
	private const float RowHeight = 32f;

	private const float SlotHeight = 36f;

	private static readonly Dictionary<LoadoutSlot, string> CountBuffers = new Dictionary<LoadoutSlot, string>();

	private const float LabelWidth = 120f;

	private const float IconWidth = 32f;

	private const float IconGap = 6f;

	private const float ControlX = 158f;

	private const float ControlWidth = 220f;

	private const float QualityWidth = 140f;

	internal static void DrawFields(Rect body, WeaponPolicy policy, ref Vector2 scrollPosition, ref float lastHeight, ref int activeTab, Pawn contextPawn) {
		// it's too small and i can't find where this damn number is being set
		//body.width = body.width + 100f;
		Text.Font = GameFont.Small;
		float y = body.y;
		y = DrawWeaponSlot(body, y, "ASA_Edit_MainHand".Translate(), policy.mainDef, policy.mainStuff, (ThingDef def, ThingDef stuff) => {
			policy.mainDef = def;
			policy.mainStuff = stuff;
			if (ArsenalRules.IsTwoHanded(def)) {
				policy.offDef = null;
				policy.offStuff = null;
			}
		}, policy.mainMinQuality, (QualityCategory q) => {
			policy.mainMinQuality = q;
		});
		if (ArmourySettings.dualWieldEnabled) {
			bool flag = ArsenalRules.IsTwoHanded(policy.mainDef);
			y = DrawWeaponSlot(body, y, "ASA_Edit_OffHand".Translate(), policy.offDef, policy.offStuff, (ThingDef def, ThingDef stuff) => {
				policy.offDef = def;
				policy.offStuff = stuff;
			}, policy.offMinQuality, (QualityCategory q) => {
				policy.offMinQuality = q;
			}, !flag, flag ? ((string)"ASA_Edit_NeedsBothHands".Translate()) : null);
		}
		y += 4f;
		Widgets.DrawLineHorizontal(body.x, y, body.width);
		y += 8f;
		Rect rect = new Rect(body.x, y, 120f, 36f);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect, "ASA_Edit_Stance".Translate());
		Text.Anchor = TextAnchor.UpperLeft;
		if (Widgets.ButtonText(new Rect(body.x + 158f, y + 3f, 220f, 28f), policy.stance.LabelKey().Translate())) {
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			foreach (CombatStance value in Enum.GetValues(typeof(CombatStance))) {
				if (value.Offerable()) {
					CombatStance choice = value;
					Action<Rect> mouseoverGuiAction = (Rect rect12) => {
						TooltipHandler.TipRegion(rect12, choice.LabelKey().Translate() + "\n\n" + choice.DescKey().Translate());
					};
					list.Add(new FloatMenuOption(choice.LabelKey().Translate(), () => {
						policy.stance = choice;
					}, StanceIcons.Get(choice), Color.white, MenuOptionPriority.Default, mouseoverGuiAction));
				}
			}
			Find.WindowStack.Add(new FloatMenu(list));
		}
		y += 40f;
		Rect rect2 = new Rect(body.x, y, 120f, 36f);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect2, "ASA_Edit_FallBackArea".Translate());
		Text.Anchor = TextAnchor.UpperLeft;
		Rect rect3 = new Rect(body.x + 158f, y + 3f, 220f, 28f);
		string label = (policy.retreatArea != null) ? policy.retreatArea.Label : ((string)"ASA_Retreat_None".Translate());
		if (Widgets.ButtonText(rect3, label)) {
			List<FloatMenuOption> list2 = new List<FloatMenuOption>
			{
				new FloatMenuOption("ASA_Retreat_None".Translate(), () =>
				{
					policy.retreatArea = null;
				})
			};
			Map currentMap = Find.CurrentMap;
			if (currentMap != null) {
				List<Area> allAreas = currentMap.areaManager.AllAreas;
				for (int num = 0; num < allAreas.Count; num++) {
					Area area = allAreas[num];
					if (area.AssignableAsAllowed()) {
						list2.Add(new FloatMenuOption(area.Label, () => {
							policy.retreatArea = area;
						}));
					}
				}
			}
			Find.WindowStack.Add(new FloatMenu(list2));
		}
		y += 40f;
		Rect rect4 = new Rect(body.x, y, 120f, 36f);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect4, "ASA_Edit_FallBack".Translate());
		Text.Anchor = TextAnchor.UpperLeft;
		Rect rect5 = new Rect(body.x + 158f, y + 6f, 130f, 22f);
		policy.retreatAtHealth = Widgets.HorizontalSlider(rect5, policy.retreatAtHealth, 0f, 0.9f, middleAlignment: false, null, null, null, 0.05f);
		Rect rect6 = new Rect(rect5.xMax + 8f, y, 80f, 36f);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect6, (policy.retreatAtHealth <= 0f) ? ((string)"ASA_Edit_Never".Translate()) : policy.retreatAtHealth.ToStringPercent());
		Text.Anchor = TextAnchor.UpperLeft;
		y += 42f;
		Widgets.DrawLineHorizontal(body.x, y, body.width);
		y += 8f;
		bool ammoAvailable = LoadoutStockDefs.AmmoAvailable;
		//bool grenadesAvailable = LoadoutStockDefs.GrenadesAvailable;
		if (activeTab == 1 && !ammoAvailable) {
			activeTab = 0;
		}
		//if (activeTab == 2 && !grenadesAvailable)
		//{
		//	activeTab = 0;
		//}
		Rect row = new Rect(body.x, y, body.width - 120f, 26f);
		float x = row.x;
		x = DrawTab(row, x, "ASA_Edit_Sidearms".Translate(), 0, ref activeTab);
		if (ammoAvailable) x = DrawTab(row, x, "ASA_Loadout_Ammo".Translate(), 1, ref activeTab);
		//if (grenadesAvailable) x = DrawTab(row, x, "ASA_Loadout_Grenades".Translate(), 2, ref activeTab);
		x = DrawTab(row, x, "ASA_Loadout_Grenades".Translate(), 2, ref activeTab);
		x = DrawTab(row, x, "ASA_Loadout_Meals".Translate(), 3, ref activeTab);
		DrawTab(row, x, "ASA_Loadout_Medicine".Translate(), 4, ref activeTab);
		List<LoadoutSlot> list3;
		switch (activeTab) {
			default: // implied 0
				list3 = policy.sidearms;
				break;
			case 1:
				list3 = policy.ammo;
				break;
			case 2:
				list3 = policy.grenades;
				break;
			case 3:
				list3 = policy.meals;
				break;
			case 4:
				list3 = policy.medicine;
				break;
		}
		bool flag2 = activeTab != 0;
		if (Widgets.ButtonText(new Rect(body.xMax - 110f, y, 100f, 26f), "ASA_Edit_Add".Translate())) {
			if (!flag2) {
				OpenWeaponPicker((ThingDef def, ThingDef stuff) => {
					policy.sidearms.Add(new LoadoutSlot {
						def = def,
						stuff = stuff
					});
				});
			} else {
				LoadoutStock kind;
				switch (activeTab) {
					default: // implied 1
						kind = LoadoutStock.Ammo;
						break;
					case 2:
						kind = LoadoutStock.Grenades;
						break;
					case 3:
						kind = LoadoutStock.Meals;
						break;
					case 4:
						kind = LoadoutStock.Medicine;
						break;
				}
				OpenStockPicker(list3, kind, policy);
			}
		}
		y += 32f;
		Rect outRect = new Rect(body.x, y, body.width, body.yMax - y);
		Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, lastHeight);
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		float num2 = 0f;
		for (int num3 = list3.Count - 1; num3 >= 0; num3--) {
			LoadoutSlot loadoutSlot = list3[num3];
			Rect rect7 = new Rect(0f, num2, viewRect.width, 32f);
			if (num3 % 2 == 1) {
				Widgets.DrawLightHighlight(rect7);
			}
			Rect rect8 = new Rect(rect7.x, rect7.y, 32f, 32f);
			if (loadoutSlot.def != null) {
				Widgets.DefIcon(rect8, loadoutSlot.def, loadoutSlot.stuff);
			}
			float width = flag2 ? (rect7.width - 32f - 210f) : (rect7.width - 32f - 96f - 140f);
			Rect rect9 = new Rect(rect8.xMax + 6f, rect7.y, width, 32f);
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect9, LabelFor(loadoutSlot.def, loadoutSlot.stuff));
			Text.Anchor = TextAnchor.UpperLeft;
			if (flag2) {

				// Subract from target count
				if (Widgets.ButtonText(new Rect(rect7.xMax - 196f, rect7.y + 3f, 24f, 26f), "-")) {
					loadoutSlot.count = (int)Math.Max(1L, (long)loadoutSlot.count - StepFor(loadoutSlot.def, activeTab));
					CountBuffers[loadoutSlot] = loadoutSlot.count.ToString();
				}

				// Count is a total target; it can exceed the item's single-stack limit.
				Rect rect10 = new Rect(rect7.xMax - 168f, rect7.y + 3f, 56f, 26f);
				if (!CountBuffers.TryGetValue(loadoutSlot, out string countBuffer)) {
					countBuffer = loadoutSlot.count.ToString();
				}
				Widgets.TextFieldNumeric<int>(rect10, ref loadoutSlot.count, ref countBuffer, 1, int.MaxValue);
				CountBuffers[loadoutSlot] = countBuffer;

				// Add to target count
				if (Widgets.ButtonText(new Rect(rect7.xMax - 108f, rect7.y + 3f, 24f, 26f), "+")) {
					loadoutSlot.count = (int)Math.Min(int.MaxValue, (long)loadoutSlot.count + StepFor(loadoutSlot.def, activeTab));
					CountBuffers[loadoutSlot] = loadoutSlot.count.ToString();
				}

			} else {
				Rect rect11 = new Rect(rect7.xMax - 86f - 200f, rect7.y + 3f, 200f, 26f);
				LoadoutSlot qualitySlot = loadoutSlot;
				if (Widgets.ButtonText(rect11, LoadoutCoverage.Label(loadoutSlot.minQuality))) {
					LoadoutCoverage.OpenPicker((QualityCategory q) => {
						qualitySlot.minQuality = q;
					});
				}
			}
			if (Widgets.ButtonText(new Rect(rect7.xMax - 80f, rect7.y + 3f, 76f, 26f), "ASA_Edit_Remove".Translate())) {
				CountBuffers.Remove(loadoutSlot);
				list3.RemoveAt(num3);
			}
			num2 += 32f;
		}
		if (list3.Count == 0) {
			string customLabel;
			if (activeTab == 0) {
				customLabel = "ASA_Edit_NoSidearms".Translate();
			} else {
				string text = "";
				switch (activeTab) {
					default: // implied 1
						text = "ASA_Loadout_Ammo";
						break;
					case 2:
						text = "ASA_Loadout_Grenades";
						break;
					case 3:
						text = "ASA_Loadout_Meals";
						break;
					case 4:
						text = "ASA_Loadout_Medicine";
						break;
				}
				customLabel = "ASA_Loadout_StockNone".Translate(text.Translate());
			}
			Widgets.NoneLabelCenteredVertically(new Rect(0f, 0f, viewRect.width, 40f), customLabel);
			num2 = 40f;
		}
		lastHeight = Mathf.Max(num2, 1f);
		Widgets.EndScrollView();
	}

	private static int StepFor(ThingDef def, int activeTab) {
		if (def == null) return 1;

		float adjustMultiplier = 1f;
		if (Input.GetKey(KeyCode.LeftControl)) adjustMultiplier *= 10f;
		if (Input.GetKey(KeyCode.LeftAlt)) adjustMultiplier *= 30f;
		if (Input.GetKey(KeyCode.LeftShift)) adjustMultiplier *= 100f;

		int step = 1;
		// honestly, just the keyboard modifiers should be fine
		/* switch (activeTab) {
			default: // implied 1
				step = 1;
				break;
			case 1: // ammo
				step = 10;
				if (def.stackLimit < 50) step = 1;
				break;
				//case 2: // grenades
				//	step = 1;
				//	break;
				//case 3: // meals
				//	step = 1;
				//	break;
				//case 4: // medicine
				//	step = 1;
				//	break;
		} */
		//return step;
		return Mathf.FloorToInt(step * adjustMultiplier);
	}

	private static float DrawTab(Rect row, float x, string label, int index, ref int activeTab) {
		float num = Mathf.Max(70f, Text.CalcSize(label).x + 20f);
		Rect rect = new Rect(x, row.y, num, row.height);
		if (activeTab == index) {
			Widgets.DrawLightHighlight(rect);
		}
		if (Widgets.ButtonText(rect, label)) {
			activeTab = index;
		}
		return x + num + 4f;
	}

	private static void OpenStockPicker(List<LoadoutSlot> slots, LoadoutStock kind, WeaponPolicy policy) {
		List<ThingDef> defs = new List<ThingDef>();
		List<ThingDef> candidates = LoadoutStockDefs.For(kind);
		for (int i = 0; i < candidates.Count; i++) {
			ThingDef def = candidates[i];
			if (!slots.Exists((LoadoutSlot slot) => slot?.def == def)) {
				defs.Add(def);
			}
		}

		HashSet<ThingDef> recommended = kind == LoadoutStock.Ammo
			? Dialog_LoadoutItemPicker.AmmoForWeapons(policy)
			: new HashSet<ThingDef>();
		defs.Sort((ThingDef a, ThingDef b) => {
			int priority = recommended.Contains(b).CompareTo(recommended.Contains(a));
			return priority != 0 ? priority : string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase);
		});
		LoadoutItemPickerSort sort = LoadoutItemPickerSort.Provided;
		if (kind == LoadoutStock.Grenades) {
			sort = LoadoutItemPickerSort.MapCountDescending;
		} else if (kind == LoadoutStock.Meals || kind == LoadoutStock.Medicine) {
			sort = LoadoutItemPickerSort.MapCountPriority;
		}
		bool searchable = true;//kind == LoadoutStock.Ammo || kind == LoadoutStock.Grenades;
		bool showCount = true;//kind == LoadoutStock.Ammo || kind == LoadoutStock.Grenades;
		Find.WindowStack.Add(new Dialog_LoadoutItemPicker(defs, (ThingDef def) => {
			slots.Add(new LoadoutSlot {
				def = def,
				count = LoadoutStockDefs.DefaultCount(def, kind)
			});
		}, searchable, showCount, searchLabel: kind == LoadoutStock.Ammo
			? ArmouryText.Get("ASA_Edit_AmmoSearch")
			: null, sort: sort));
	}

	private static float DrawWeaponSlot(Rect body, float y, string label, ThingDef def, ThingDef stuff, Action<ThingDef, ThingDef> set, QualityCategory minQuality, Action<QualityCategory> setQuality, bool enabled = true, string disabledReason = null) {
		Rect rect = new Rect(body.x, y, 120f, 36f);
		Text.Anchor = TextAnchor.MiddleLeft;
		GUI.color = enabled ? Color.white : Color.gray;
		Widgets.Label(rect, label);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		Rect rect2 = new Rect(body.x + 120f, y + 2f, 32f, 32f);
		if (enabled && def != null) {
			Widgets.DefIcon(rect2, def, stuff);
		}
		Rect rect3 = new Rect(body.x + 158f, y + 4f, 220f, 28f);
		string label2 = (enabled ? LabelFor(def, stuff) : disabledReason);
		if (Widgets.ButtonText(rect3, label2, drawBackground: true, doMouseoverSound: true, enabled) & enabled) {
			OpenWeaponPicker(set, allowNone: true);
		}
		if (enabled && def != null && Widgets.ButtonText(new Rect(rect3.xMax + 8f, y + 4f, 200f, 28f), LoadoutCoverage.Label(minQuality))) {
			LoadoutCoverage.OpenPicker(setQuality);
		}
		return y + 36f + 4f;
	}

	private static string LabelFor(ThingDef def, ThingDef stuff) {
		if (def == null) {
			return "ASA_Edit_Empty".Translate();
		}
		return def.label.CapitalizeFirst();
	}

	private static void OpenWeaponPicker(Action<ThingDef, ThingDef> set, bool allowNone = false) {
		List<ThingDef> defs = new List<ThingDef>();
		List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
		for (int i = 0; i < allDefs.Count; i++) {
			if (IsWeapon(allDefs[i])) {
				defs.Add(allDefs[i]);
			}
		}
		defs.Sort((ThingDef a, ThingDef b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase));
		Find.WindowStack.Add(new Dialog_LoadoutItemPicker(defs, (ThingDef def) => set(def, null),
			searchable: true, showMapCount: false, allowNone: allowNone, selectNone: () => set(null, null),
			sort: LoadoutItemPickerSort.MapCountPriority));
	}

	private static bool IsWeapon(ThingDef def) {
		return ArsenalRules.IsWieldable(def);
	}

	internal static void ReapplyToWearers(WeaponPolicy policy) {
		foreach (Map map in Find.Maps) {
			IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
			for (int i = 0; i < allPawnsSpawned.Count; i++) {
				CompArsenal compArsenal = CompArsenal.For(allPawnsSpawned[i]);
				if (compArsenal != null && compArsenal.policy == policy) {
					PolicyApplier.Apply(allPawnsSpawned[i], policy);
				}
			}
		}
	}
}
