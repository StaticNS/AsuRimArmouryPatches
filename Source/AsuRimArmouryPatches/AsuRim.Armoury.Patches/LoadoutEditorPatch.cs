using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury.Patches;

/// <summary>
/// Patches the loadout editor to add support for ammo, grenades, meals, and medicine.
/// Although ammo and meals is part of the original mod, we're messing around with it so..
/// </summary>
internal static class LoadoutEditorPatch {

	private static readonly Dictionary<LoadoutSlot, string> CountBuffers = new Dictionary<LoadoutSlot, string>();

	private static bool missingCEStatsLogged;
	private static bool policyWindowResizeLogged;

	/// <summary>
	/// Sets the initial size and position of the loadout editor window to a more reasonable size.
	/// This is a postfix patch to the constructor of the loadout editor window.
	/// </summary>
	/// <param name="__instance"></param>
	internal static void SetInitialSizeAndPositionPostfix(Window __instance) {
		if (__instance.GetType().FullName != "AsuRim.Armoury.Dialog_ManageLoadoutPolicies") {
			return;
		}
		float widthScale = Screen.windowWidth / 2560f;
		float heightScale = Screen.windowHeight / 1440f;
		float width = 960f * (Screen.windowWidth / 2560f);
		float height = 720f * (Screen.windowHeight / 1440f);
		Rect windowRect = __instance.windowRect;
		Vector2 center = windowRect.center;
		__instance.windowRect = new Rect(center.x - width / 2f, center.y - height / 2f, width, height);
		if (!policyWindowResizeLogged) {
			Log.Message($"[AsuRimArmouryPatches] Policy window resized to {width:0}x{height}.");
			policyWindowResizeLogged = true;
		}
	}

	/// <summary>
	/// Draws the loadout editor window with support for ammo, grenades, meals, and medicine.
	/// This is a prefix patch to the DoWindowContents method of the loadout editor window
	/// </summary>
	/// <param name="body"></param>
	/// <param name="policy"></param>
	/// <param name="scrollPosition"></param>
	/// <param name="lastHeight"></param>
	/// <param name="activeTab"></param>
	/// <param name="contextPawn"></param>
	/// <returns></returns>
	internal static bool Prefix(
		Rect body,
		WeaponPolicy policy,
		ref Vector2 scrollPosition,
		ref float lastHeight,
		ref int activeTab,
		Pawn contextPawn
	) {
		DrawFields(body, policy, ref scrollPosition, ref lastHeight, ref activeTab);
		return false;
	}

	/// <summary>
	/// Draws the fields for the loadout editor window, including weapon slots, stance, fallback area, and stock tabs.
	/// This method is called by the Prefix patch to the DoWindowContents method of the loadout editor window.
	/// I'd rather not have to do this, but the original mod doesn't provide a way to extend the window contents, so we have to replace it entirely.
	/// Kill me.
	/// </summary>
	/// <param name="body"></param>
	/// <param name="policy"></param>
	/// <param name="scrollPosition"></param>
	/// <param name="lastHeight"></param>
	/// <param name="activeTab"></param>
	private static void DrawFields(Rect body, WeaponPolicy policy, ref Vector2 scrollPosition, ref float lastHeight, ref int activeTab) {
		Text.Font = GameFont.Small;
		float y = body.y;
		// Draw the main hand weapon slot
		y = DrawWeaponSlot(body, y, "ASA_Edit_MainHand".Translate(), policy.mainDef, policy.mainStuff, (def, stuff) => {
			policy.mainDef = def;
			policy.mainStuff = stuff;
			if (ArsenalRules.IsTwoHanded(def)) {
				policy.offDef = null;
				policy.offStuff = null;
			}
		}, policy.mainMinQuality, q => policy.mainMinQuality = q);

		// Draw the off hand weapon slot if dual wielding is enabled
		if (ArmourySettings.dualWieldEnabled) {
			bool twoHanded = ArsenalRules.IsTwoHanded(policy.mainDef);
			y = DrawWeaponSlot(body, y, "ASA_Edit_OffHand".Translate(), policy.offDef, policy.offStuff,
				(def, stuff) => {
					policy.offDef = def;
					policy.offStuff = stuff;
				}, policy.offMinQuality, q => policy.offMinQuality = q, !twoHanded,
				twoHanded ? "ASA_Edit_NeedsBothHands".Translate() : null);
		}

		y += 4f;
		// separator line yippee. it looks so good!
		Widgets.DrawLineHorizontal(body.x, y, body.width);
		y += 8f;
		Text.Anchor = TextAnchor.MiddleLeft;
		// stance shenanigans
		Widgets.Label(new Rect(body.x, y, 120f, 36f), "ASA_Edit_Stance".Translate());
		Text.Anchor = TextAnchor.UpperLeft;
		if (Widgets.ButtonText(new Rect(body.x + 158f, y + 3f, 220f, 28f), policy.stance.LabelKey().Translate())) {
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			foreach (CombatStance value in Enum.GetValues(typeof(CombatStance))) {
				if (!value.Offerable()) {
					continue;
				}
				CombatStance choice = value;
				Action<Rect> mouseover = rect =>
					TooltipHandler.TipRegion(rect, choice.LabelKey().Translate() + "\n\n" + choice.DescKey().Translate());
				options.Add(new FloatMenuOption(choice.LabelKey().Translate(), () => policy.stance = choice,
					StanceIcons.Get(choice), Color.white, MenuOptionPriority.Default, mouseover));
			}
			Find.WindowStack.Add(new FloatMenu(options));
		}

		// onto our fallback selection bullshit! yay!
		y += 40f;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(new Rect(body.x, y, 120f, 36f), "ASA_Edit_FallBackArea".Translate());
		Text.Anchor = TextAnchor.UpperLeft;
		Rect areaRect = new Rect(body.x + 158f, y + 3f, 220f, 28f);
		string areaLabel = policy.retreatArea != null ? policy.retreatArea.Label.ToString() : "ASA_Retreat_None".Translate().ToString();
		if (Widgets.ButtonText(areaRect, areaLabel)) {
			List<FloatMenuOption> options = new List<FloatMenuOption>
			{
				new FloatMenuOption("ASA_Retreat_None".Translate(), () => policy.retreatArea = null)
			};
			Map currentMap = Find.CurrentMap;
			if (currentMap != null) {
				List<Area> areas = currentMap.areaManager.AllAreas;
				for (int i = 0; i < areas.Count; i++) {
					Area area = areas[i];
					if (area.AssignableAsAllowed()) {
						options.Add(new FloatMenuOption(area.Label, () => policy.retreatArea = area));
					}
				}
			}
			Find.WindowStack.Add(new FloatMenu(options));
		}

		y += 40f;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(new Rect(body.x, y, 120f, 36f), "ASA_Edit_FallBack".Translate());
		Text.Anchor = TextAnchor.UpperLeft;
		// retreat health threshold slider.
		Rect healthRect = new Rect(body.x + 158f, y + 6f, 130f, 22f);
		policy.retreatAtHealth = Widgets.HorizontalSlider(healthRect, policy.retreatAtHealth, 0f, 0.9f, false, null, null, null, 0.05f);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(new Rect(healthRect.xMax + 8f, y, 80f, 36f),
			policy.retreatAtHealth <= 0f ? "ASA_Edit_Never".Translate().ToString() : policy.retreatAtHealth.ToStringPercent());
		Text.Anchor = TextAnchor.UpperLeft;

		y += 42f;
		// another separator line. i love these things.
		Widgets.DrawLineHorizontal(body.x, y, body.width);
		y += 8f;
		// if someone, somehow, manages to get to the ammo tab when ammo is disabled, we need to switch them back to the sidearms tab.
		// don't wanna break anything too hard.
		if (activeTab == 1 && !PatchLoadoutStockDefs.AmmoAvailable) {
			activeTab = 0;
		}

		// our glorious stock tabs! ammo, grenades, meals, and medicine. oh my!
		Rect tabsRect = new Rect(body.x, y, body.width - 120f, 26f);
		float x = tabsRect.x;
		x = DrawTab(tabsRect, x, "ASA_Edit_Sidearms".Translate(), 0, ref activeTab);
		if (PatchLoadoutStockDefs.AmmoAvailable) {
			x = DrawTab(tabsRect, x, "ASA_Loadout_Ammo".Translate(), 1, ref activeTab);
		}
		x = DrawTab(tabsRect, x, "ASA_Loadout_Grenades".Translate(), 2, ref activeTab);
		x = DrawTab(tabsRect, x, "ASA_Loadout_Meals".Translate(), 3, ref activeTab);
		DrawTab(tabsRect, x, "ASA_Loadout_Medicine".Translate(), 4, ref activeTab);

		PolicyLoadoutExtension extension = PolicyLoadoutExtension.For(policy);
		List<LoadoutSlot> slots = activeTab switch {
			1 => policy.ammo,
			2 => extension.Grenades,
			3 => policy.meals,
			4 => extension.Medicine,
			_ => policy.sidearms
		};

		// our add button. opens our weapon picker for sidearms. opens our stock picker for ammo, grenades, meals, and medicine. easy peasy.
		bool stockTab = activeTab != 0;
		if (Widgets.ButtonText(new Rect(body.xMax - 110f, y, 100f, 26f), "ASA_Edit_Add".Translate())) {
			if (!stockTab) {
				OpenWeaponPicker((def, stuff) => policy.sidearms.Add(new LoadoutSlot { def = def, stuff = stuff }));
			} else {
				PatchLoadoutStock kind = activeTab switch {
					2 => PatchLoadoutStock.Grenades,
					3 => PatchLoadoutStock.Meals,
					4 => PatchLoadoutStock.Medicine,
					_ => PatchLoadoutStock.Ammo
				};
				OpenStockPicker(slots, kind, policy);
			}
		}

		// CE has a more robust weight and bulk system. It'd be nice to know our weight and bulk of our loadout.
		// So we're now doing that!
		y += 32f;
		Rect outRect = new Rect(body.x, y, body.width, body.yMax - y);
		bool showCETotals = ModsConfig.IsActive("CETeam.CombatExtended")
			&& DefDatabase<StatDef>.GetNamedSilentFail("Mass") != null
			&& DefDatabase<StatDef>.GetNamedSilentFail("Bulk") != null;
		if (ModsConfig.IsActive("CETeam.CombatExtended") && !showCETotals && !missingCEStatsLogged) {
			Log.Error("[AsuRimArmouryPatches] Combat Extended is active but its Mass or Bulk stat is unavailable; loadout totals are disabled.");
			missingCEStatsLogged = true;
		}
		if (showCETotals) {
			outRect.height = Mathf.Max(0f, outRect.height - 26f);
		}

		// (imagine this as a dark souls player left message) mess. below.
		Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, lastHeight);
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		float rowY = 0f;
		// loop over the number of items added to our policy
		// add the widgets for eacth one.
		for (int i = slots.Count - 1; i >= 0; i--) {
			LoadoutSlot slot = slots[i];
			Rect row = new Rect(0f, rowY, viewRect.width, 32f);
			// more odd row readability stuff.
			if (i % 2 == 1) {
				Widgets.DrawLightHighlight(row);
			}
			// icon
			Rect iconRect = new Rect(row.x, row.y, 32f, 32f);
			if (slot.def != null) {
				Widgets.DefIcon(iconRect, slot.def, slot.stuff);
			}
			// label and count
			float labelWidth = stockTab ? row.width - 32f - 210f : row.width - 32f - 96f - 140f;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(iconRect.xMax + 6f, row.y, labelWidth, 32f), LabelFor(slot.def));
			Text.Anchor = TextAnchor.UpperLeft;
			if (stockTab) {
				// for stock tabs, we have a step button for adding and removing target amounts.
				// we also now have a text field to make life better.
				if (Widgets.ButtonText(new Rect(row.xMax - 196f, row.y + 3f, 24f, 26f), "-")) {
					slot.count = (int)Math.Max(1L, (long)slot.count - StepFor(slot.def));
					CountBuffers[slot] = slot.count.ToString();
				}
				Rect countRect = new Rect(row.xMax - 168f, row.y + 3f, 56f, 26f);
				if (!CountBuffers.TryGetValue(slot, out string buffer)) {
					buffer = slot.count.ToString();
				}
				Widgets.TextFieldNumeric<int>(countRect, ref slot.count, ref buffer, 1, int.MaxValue);
				CountBuffers[slot] = buffer;
				if (Widgets.ButtonText(new Rect(row.xMax - 108f, row.y + 3f, 24f, 26f), "+")) {
					slot.count = (int)Math.Min(int.MaxValue, (long)slot.count + StepFor(slot.def));
					CountBuffers[slot] = slot.count.ToString();
				}
			} else {
				// for the sidearms tabs, we won't exactly need to equip 20 grenade launchers.
				// also there's no quality for your insect jelly. I hope
				LoadoutSlot qualitySlot = slot;
				Rect qualityRect = new Rect(row.xMax - 86f - 200f, row.y + 3f, 200f, 26f);
				if (Widgets.ButtonText(qualityRect, LoadoutCoverage.Label(slot.minQuality))) {
					LoadoutCoverage.OpenPicker(q => qualitySlot.minQuality = q);
				}
			}
			// remove button
			if (Widgets.ButtonText(new Rect(row.xMax - 80f, row.y + 3f, 76f, 26f), "ASA_Edit_Remove".Translate())) {
				CountBuffers.Remove(slot);
				slots.RemoveAt(i);
			}
			rowY += 32f;
		}

		// say things are empty when they're empty. it makes sense, right? i think so.
		if (slots.Count == 0) {
			string emptyLabel = activeTab == 0
				? "ASA_Edit_NoSidearms".Translate()
				: "ASA_Loadout_StockNone".Translate(TabLabel(activeTab));
			Widgets.NoneLabelCenteredVertically(new Rect(0f, 0f, viewRect.width, 40f), emptyLabel);
			rowY = 40f;
		}
		// record our last scroll height. quality of life! I don't think this is every going to be used!
		// but it's in the orignal mod, so I'm including it. how genourous of me.
		lastHeight = Mathf.Max(rowY, 1f);
		Widgets.EndScrollView();
		// funny CE stats
		if (showCETotals) {
			DrawCETotals(body, policy, extension);
		}
	}

	/// <summary>
	/// Draws the total mass and bulk of the loadout, if Combat Extended is active and its Mass and Bulk stats are available.
	/// </summary>
	/// <param name="body"></param>
	/// <param name="policy"></param>
	/// <param name="extension"></param>
	private static void DrawCETotals(Rect body, WeaponPolicy policy, PolicyLoadoutExtension extension) {
		StatDef mass = DefDatabase<StatDef>.GetNamed("Mass");
		StatDef bulk = DefDatabase<StatDef>.GetNamed("Bulk");
		float totalMass = 0f;
		float totalBulk = 0f;
		// i love references. it makes me feel like a real programmer. like i'm at home programming in rust or c++. but no, i'm in c#. the anals of the programming world. fuck microslop
		AddItem(policy.mainDef, policy.mainStuff, 1, mass, bulk, ref totalMass, ref totalBulk);
		AddItem(policy.offDef, policy.offStuff, 1, mass, bulk, ref totalMass, ref totalBulk);
		AddSlots(policy.sidearms, false, mass, bulk, ref totalMass, ref totalBulk);
		AddSlots(policy.ammo, true, mass, bulk, ref totalMass, ref totalBulk);
		AddSlots(extension.Grenades, true, mass, bulk, ref totalMass, ref totalBulk);
		AddSlots(policy.meals, true, mass, bulk, ref totalMass, ref totalBulk);
		AddSlots(extension.Medicine, true, mass, bulk, ref totalMass, ref totalBulk);

		Rect footer = new Rect(body.x, body.yMax - 22f, body.width, 22f);
		Widgets.DrawLineHorizontal(footer.x, footer.y, footer.width);
		Text.Anchor = TextAnchor.MiddleRight;
		Widgets.Label(footer, "ASA_Loadout_CETotals".Translate(totalMass.ToString("F1"), totalBulk.ToString("F1")));
		Text.Anchor = TextAnchor.UpperLeft;
	}

	/// <summary>
	/// Helpers for getting the list of items for each stock type, and the default count for each stock type.
	/// </summary>
	/// <param name="slots"></param>
	/// <param name="useConfiguredCount"></param>
	/// <param name="mass"></param>
	/// <param name="bulk"></param>
	/// <param name="totalMass"></param>
	/// <param name="totalBulk"></param>
	private static void AddSlots(
		List<LoadoutSlot> slots,
		bool useConfiguredCount,
		StatDef mass,
		StatDef bulk,
		ref float totalMass,
		ref float totalBulk) {
		if (slots == null) {
			return;
		}
		for (int i = 0; i < slots.Count; i++) {
			LoadoutSlot slot = slots[i];
			if (slot != null) {
				int count = useConfiguredCount ? Math.Max(0, slot.count) : 1;
				AddItem(slot.def, slot.stuff, count, mass, bulk, ref totalMass, ref totalBulk);
			}
		}
	}

	/// <summary>
	/// Helpers for adding the mass and bulk of a single item to the totals.
	/// </summary>
	/// <param name="def"></param>
	/// <param name="stuff"></param>
	/// <param name="count"></param>
	/// <param name="mass"></param>
	/// <param name="bulk"></param>
	/// <param name="totalMass"></param>
	/// <param name="totalBulk"></param>
	private static void AddItem(
		ThingDef def,
		ThingDef stuff,
		int count,
		StatDef mass,
		StatDef bulk,
		ref float totalMass,
		ref float totalBulk) {
		if (def == null || count <= 0) {
			return;
		}
		totalMass += def.GetStatValueAbstract(mass, stuff) * count;
		totalBulk += def.GetStatValueAbstract(bulk, stuff) * count;
	}

	/// <summary>
	/// Returns the label for a tab based on its index.
	/// </summary>
	/// <param name="tab"></param>
	/// <returns></returns>
	private static string TabLabel(int tab) {
		switch (tab) {
			case 1:
				return "ASA_Loadout_Ammo".Translate();
			case 2:
				return "ASA_Loadout_Grenades".Translate();
			case 3:
				return "ASA_Loadout_Meals".Translate();
			default:
				return "ASA_Loadout_Medicine".Translate();
		}
	}

	/// <summary>
	/// Returns the step size for adjusting the count of an item based on modifier keys.<br/><br/>
	/// If no modifier keys are pressed, the step size is 1.<br/>
	/// If Left Control is pressed, the step size is 10.<br/>
	/// If Left Alt is pressed, the step size is 30.<br/>
	/// If Left Shift is pressed, the step size is 100.<br/><br/>
	/// Conveniently, they compound multiplicatively, so if you hold down Left Control and Left Shift, the step size is 1000.
	/// </summary>
	/// <param name="def"></param>
	/// <returns></returns>
	private static int StepFor(ThingDef def) {
		if (def == null) {
			return 1;
		}
		int multiplier = 1;
		if (Input.GetKey(KeyCode.LeftControl)) {
			multiplier *= 10;
		}
		if (Input.GetKey(KeyCode.LeftAlt)) {
			multiplier *= 30;
		}
		if (Input.GetKey(KeyCode.LeftShift)) {
			multiplier *= 100;
		}
		return multiplier;
	}

	/// <summary>
	/// Draws a tab button for the loadout editor window and returns the new x position for the next tab.
	/// </summary>
	/// <param name="row"></param>
	/// <param name="x"></param>
	/// <param name="label"></param>
	/// <param name="index"></param>
	/// <param name="activeTab"></param>
	/// <returns></returns>
	private static float DrawTab(Rect row, float x, string label, int index, ref int activeTab) {
		float width = Mathf.Max(70f, Text.CalcSize(label).x + 20f);
		Rect rect = new Rect(x, row.y, width, row.height);
		if (activeTab == index) {
			Widgets.DrawLightHighlight(rect);
		}
		if (Widgets.ButtonText(rect, label)) {
			activeTab = index;
		}
		return x + width + 4f;
	}

	/// <summary>
	/// Opens the stock picker dialog for the specified stock type and adds the selected item to the specified list of loadout slots.
	/// </summary>
	/// <param name="slots"></param>
	/// <param name="kind"></param>
	/// <param name="policy"></param>
	private static void OpenStockPicker(List<LoadoutSlot> slots, PatchLoadoutStock kind, WeaponPolicy policy) {
		List<ThingDef> defs = new List<ThingDef>();
		// our magia vandour candidates. I'm obssessed with Wistoria: Wand and Sword
		// I really want more of it to be written. When I finished season 2, I was excited to read the light novel.
		// the source material is NOT a light novel. It's manga. Fuck
		// Don't get me wrong, I love manga too, but I would've preffered a ln
		// usually the lns are miles ahead of the anime
		// alas. i read the rest of the manga in a short amount of time.
		// what is this variable for again?
		List<ThingDef> candidates = PatchLoadoutStockDefs.For(kind);
		for (int i = 0; i < candidates.Count; i++) {
			ThingDef def = candidates[i];
			if (!slots.Exists(slot => slot?.def == def)) {
				defs.Add(def);
			}
		}

		// sometimes we really strongly believe the player wants specific ammo.
		// so we can recommend them to the player. this is a nice quality of life feature. for me. your god.
		HashSet<ThingDef> recommended = kind == PatchLoadoutStock.Ammo
			? Dialog_LoadoutItemPicker.AmmoForWeapons(policy)
			: new HashSet<ThingDef>();
		defs.Sort((a, b) => {
			int priority = recommended.Contains(b).CompareTo(recommended.Contains(a));
			return priority != 0
				? priority
				: string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase);
		});
		// grenades get to be sorted exclusively by map count. lucky them.
		LoadoutItemPickerSort sort = kind == PatchLoadoutStock.Grenades
			? LoadoutItemPickerSort.MapCountDescending
			: kind == PatchLoadoutStock.Meals || kind == PatchLoadoutStock.Medicine
				? LoadoutItemPickerSort.MapCountPriority
				: LoadoutItemPickerSort.Provided;
		Find.WindowStack.Add(new Dialog_LoadoutItemPicker(defs, def => {
			slots.Add(new LoadoutSlot { def = def, count = PatchLoadoutStockDefs.DefaultCount(def, kind) });
		}, searchable: true, showMapCount: true,
			searchLabel: kind == PatchLoadoutStock.Ammo ? "ASA_Edit_AmmoSearch".Translate() : null,
			sort: sort));
	}

	/// <summary>
	/// Draws a weapon slot for the loadout editor window, including the label, icon, button, and quality selector.
	/// </summary>
	/// <param name="body"></param>
	/// <param name="y"></param>
	/// <param name="label"></param>
	/// <param name="def"></param>
	/// <param name="stuff"></param>
	/// <param name="set"></param>
	/// <param name="minQuality"></param>
	/// <param name="setQuality"></param>
	/// <param name="enabled"></param>
	/// <param name="disabledReason"></param>
	/// <returns></returns>
	private static float DrawWeaponSlot(
		Rect body,
		float y,
		string label,
		ThingDef def,
		ThingDef stuff,
		Action<ThingDef, ThingDef> set,
		QualityCategory minQuality,
		Action<QualityCategory> setQuality,
		bool enabled = true,
		string disabledReason = null
	) {
		Rect labelRect = new Rect(body.x, y, 120f, 36f);
		Text.Anchor = TextAnchor.MiddleLeft;
		GUI.color = enabled ? Color.white : Color.gray;
		Widgets.Label(labelRect, label);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		Rect iconRect = new Rect(body.x + 120f, y + 2f, 32f, 32f);
		if (enabled && def != null) {
			Widgets.DefIcon(iconRect, def, stuff);
		}
		Rect buttonRect = new Rect(body.x + 158f, y + 4f, 220f, 28f);
		string buttonLabel = enabled ? LabelFor(def) : disabledReason;
		if (Widgets.ButtonText(buttonRect, buttonLabel, true, true, enabled) && enabled) {
			OpenWeaponPicker(set, allowNone: true);
		}
		if (enabled && def != null && Widgets.ButtonText(
			new Rect(buttonRect.xMax + 8f, y + 4f, 200f, 28f), LoadoutCoverage.Label(minQuality))) {
			LoadoutCoverage.OpenPicker(setQuality);
		}
		return y + 40f;
	}

	/// <summary>
	/// Returns the label for an item, or "Empty" if the def is null.
	/// </summary>
	/// <param name="def"></param>
	/// <returns></returns>
	private static string LabelFor(ThingDef def) {
		return def == null ? "ASA_Edit_Empty".Translate() : def.label.CapitalizeFirst();
	}

	/// <summary>
	/// Opens the weapon picker dialog and adds the selected weapon to the specified loadout slot.
	/// This method is called when the user clicks the "Add" button in the loadout editor window for sidearms.
	/// It filters the available weapons to only include those that are wieldable by the pawn, and sorts them by label.
	/// It also allows the user to select a stuff material for the weapon, if applicable. Apparently. I never encountered this feature.<br/><br/>
	/// I think this still fetches weapons that you can't hold? Like, oh I don't know, a fucking howitzer.
	/// I'll get around to fixing that later. I don't think it's a big deal.
	/// </summary>
	/// <param name="set"></param>
	/// <param name="allowNone"></param>
	private static void OpenWeaponPicker(Action<ThingDef, ThingDef> set, bool allowNone = false) {
		List<ThingDef> defs = new List<ThingDef>();
		List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
		for (int i = 0; i < allDefs.Count; i++) {
			if (ArsenalRules.IsWieldable(allDefs[i])) {
				defs.Add(allDefs[i]);
			}
		}
		defs.Sort((a, b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase));
		Find.WindowStack.Add(new Dialog_LoadoutItemPicker(defs, def => set(def, null),
			searchable: true, showMapCount: false, allowNone: allowNone, selectNone: () => set(null, null),
			sort: LoadoutItemPickerSort.MapCountPriority));
	}
}
