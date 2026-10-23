using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

internal enum LoadoutItemPickerSort {
	Provided,
	MapCountPriority,
	MapCountDescending
}

internal class Dialog_LoadoutItemPicker : Window {
	private const float RowHeight = 36f;

	private readonly List<ThingDef> defs;

	private readonly Dictionary<ThingDef, int> mapCounts;

	private readonly Action<ThingDef> select;

	private readonly bool searchable;

	private readonly bool showMapCount;

	private readonly bool allowNone;

	private readonly Action selectNone;

	private readonly string searchLabel;

	private Vector2 scrollPosition;

	private string search = "";

	public override Vector2 InitialSize => new Vector2(520f, 620f);

	internal Dialog_LoadoutItemPicker(List<ThingDef> defs, Action<ThingDef> select,
		bool searchable, bool showMapCount, bool allowNone = false, Action selectNone = null, string searchLabel = null,
		LoadoutItemPickerSort sort = LoadoutItemPickerSort.Provided) {
		this.defs = defs ?? new List<ThingDef>();
		this.select = select;
		this.searchable = searchable;
		this.showMapCount = showMapCount;
		this.allowNone = allowNone;
		this.selectNone = selectNone;
		this.searchLabel = searchLabel ?? "ASA_Post_Search".Translate();
		mapCounts = CountOnMaps(this.defs);
		if (sort != LoadoutItemPickerSort.Provided) {
			this.defs.Sort((ThingDef a, ThingDef b) => {
				mapCounts.TryGetValue(a, out int countA);
				mapCounts.TryGetValue(b, out int countB);
				int countComparison = sort == LoadoutItemPickerSort.MapCountDescending
					? countB.CompareTo(countA)
					: (countB > 0).CompareTo(countA > 0);
				return countComparison != 0
					? countComparison
					: string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase);
			});
		}
		doCloseX = true;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = true;
	}

	public override void DoWindowContents(Rect inRect) {
		Text.Font = GameFont.Small;
		Rect listRect = inRect;
		if (searchable) {
			Rect searchRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
			search = Widgets.TextField(searchRect, search);
			if (search.NullOrEmpty()) {
				Text.Anchor = TextAnchor.MiddleLeft;
				GUI.color = new Color(1f, 1f, 1f, 0.4f);
				Widgets.Label(new Rect(searchRect.x + 6f, searchRect.y, searchRect.width - 12f, searchRect.height), searchLabel);
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			listRect.yMin += searchRect.height + 8f;
		}

		if (allowNone) {
			if (Widgets.ButtonText(new Rect(listRect.x, listRect.y, listRect.width, RowHeight), "ASA_Edit_Empty".Translate())) {
				selectNone?.Invoke();
				Close();
			}
			listRect.yMin += RowHeight + 4f;
		}

		int visibleCount = 0;
		for (int i = 0; i < defs.Count; i++) {
			if (Matches(defs[i])) {
				visibleCount++;
			}
		}
		Rect outRect = new Rect(listRect.x, listRect.y, listRect.width, listRect.height);
		Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Math.Max(outRect.height, visibleCount * RowHeight));
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		float y = 0f;
		for (int i = 0; i < defs.Count; i++) {
			ThingDef def = defs[i];
			if (!Matches(def)) {
				continue;
			}
			DrawRow(def, new Rect(0f, y, viewRect.width, RowHeight), i);
			y += RowHeight;
		}
		if (visibleCount == 0) {
			Widgets.NoneLabelCenteredVertically(new Rect(0f, 0f, viewRect.width, 40f), "No matches".Translate());
		}
		Widgets.EndScrollView();
	}

	private void DrawRow(ThingDef def, Rect rect, int index) {
		if (index % 2 == 1) {
			Widgets.DrawLightHighlight(rect);
		}
		int count = mapCounts.TryGetValue(def, out int available) ? available : 0;
		GUI.color = count > 0 ? Color.white : Color.gray;
		Rect iconRect = new Rect(rect.x + 2f, rect.y + 2f, 32f, 32f);
		Widgets.DefIcon(iconRect, def);
		string label = def.LabelCap.ToString();
		if (showMapCount) {
			label += " (" + count + ")";
		}
		Rect labelRect = new Rect(iconRect.xMax + 8f, rect.y, rect.width - 46f, rect.height);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(labelRect, label);
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;

		if (!Mouse.IsOver(rect)) {
			return;
		}
		Widgets.DrawHighlight(rect);
		TooltipHandler.TipRegion(rect, label);
		//TooltipHandler.TipRegion(rect, count > 0
		//	? label
		//	: label + "\n\n" + ArmouryText.Get("ASA_Edit_NotOnMap"));
		if (Widgets.ButtonInvisible(rect)) {
			select?.Invoke(def);
			Close();
		}
	}

	private bool Matches(ThingDef def) {
		return search.NullOrEmpty()
			|| def.LabelCap.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
			|| def.defName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static Dictionary<ThingDef, int> CountOnMaps(List<ThingDef> defs) {
		HashSet<ThingDef> wanted = new HashSet<ThingDef>(defs);
		Dictionary<ThingDef, int> counts = new Dictionary<ThingDef, int>();
		foreach (Map map in Find.Maps) {
			List<Thing> things = map.listerThings.AllThings;
			for (int i = 0; i < things.Count; i++) {
				Thing thing = things[i];
				if (wanted.Contains(thing.def)) {
					counts.TryGetValue(thing.def, out int count);
					counts[thing.def] = count + thing.stackCount;
				}
			}
		}
		return counts;
	}

	internal static HashSet<ThingDef> AmmoForWeapons(WeaponPolicy policy) {
		HashSet<ThingDef> ammo = new HashSet<ThingDef>();
		if (policy == null) {
			return ammo;
		}
		AddWeaponAmmo(policy.mainDef, ammo);
		AddWeaponAmmo(policy.offDef, ammo);
		for (int i = 0; i < policy.sidearms.Count; i++) {
			AddWeaponAmmo(policy.sidearms[i]?.def, ammo);
		}
		return ammo;
	}

	private static void AddWeaponAmmo(ThingDef weapon, HashSet<ThingDef> ammo) {
		if (weapon?.comps == null) {
			return;
		}
		for (int i = 0; i < weapon.comps.Count; i++) {
			object comp = weapon.comps[i];
			if (comp.GetType().Name != "CompProperties_AmmoUser") {
				continue;
			}
			AddAmmoSet(ReadMember(comp, "ammoSet"), ammo);
			object ammoSets = ReadMember(comp, "ammoSetDefs");
			if (ammoSets is System.Collections.IEnumerable enumerable) {
				foreach (object set in enumerable) {
					AddAmmoSet(set, ammo);
				}
			}
		}
	}

	private static void AddAmmoSet(object ammoSet, HashSet<ThingDef> ammo) {
		object ammoTypes = ReadMember(ammoSet, "ammoTypes");
		if (ammoTypes is not System.Collections.IEnumerable enumerable) {
			return;
		}
		foreach (object ammoLink in enumerable) {
			if (ReadMember(ammoLink, "ammo") is ThingDef ammoDef) {
				ammo.Add(ammoDef);
			}
		}
	}

	private static object ReadMember(object instance, string name) {
		if (instance == null) {
			return null;
		}
		Type type = instance.GetType();
		FieldInfo field = AccessTools.Field(type, name);
		if (field != null) {
			return field.GetValue(instance);
		}
		PropertyInfo property = AccessTools.Property(type, name);
		return property?.GetValue(instance);
	}
}
