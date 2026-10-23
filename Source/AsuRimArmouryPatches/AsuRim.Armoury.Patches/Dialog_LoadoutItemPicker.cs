using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury.Patches;

/// <summary>
/// Sort method for the <see cref="Dialog_LoadoutItemPicker"/> window.
/// </summary>
internal enum LoadoutItemPickerSort {
	Provided,
	MapCountPriority,
	MapCountDescending
}

/// <summary>
/// A dialog window that allows the user to search and select items for their loadouts.
/// </summary>
internal sealed class Dialog_LoadoutItemPicker : Window {
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
	private string search = string.Empty;

	public override Vector2 InitialSize => new Vector2(520f, 620f);

	/// <summary>
	/// Good ol' constructor. Initializes the dialog with the given parameters.
	/// That much should be obvious, as this is a constructor. Dumbass (jk, love you!).
	/// </summary>
	internal Dialog_LoadoutItemPicker(
		List<ThingDef> defs,
		Action<ThingDef> select,
		bool searchable,
		bool showMapCount,
		bool allowNone = false,
		Action selectNone = null,
		string searchLabel = null,
		LoadoutItemPickerSort sort = LoadoutItemPickerSort.Provided
	) {
		this.defs = defs ?? new List<ThingDef>();
		this.select = select;
		this.searchable = searchable;
		this.showMapCount = showMapCount;
		this.allowNone = allowNone;
		this.selectNone = selectNone;
		this.searchLabel = searchLabel ?? "ASA_Post_Search".Translate();

		// Archaic sorting code.
		mapCounts = CountOnMaps(this.defs);
		if (sort != LoadoutItemPickerSort.Provided) {
			this.defs.Sort((a, b) => {
				// grab our counts of the two items we're comparing
				mapCounts.TryGetValue(a, out int countA);
				mapCounts.TryGetValue(b, out int countB);
				// if our sort is set to MapCountDescending, then we just check b against a.
				// other wise, we just compare if there's counts at all.
				int comparison = sort == LoadoutItemPickerSort.MapCountDescending
					? countB.CompareTo(countA)
					: (countB > 0).CompareTo(countA > 0);
				// if we found a difference, great! return the difference.
				// other wise it's boring old alphabetical sorting.
				return comparison != 0
					? comparison
					: string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase);
			});
		}

		// closing actions blehj
		doCloseX = true;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = true;
	}

	/// <summary>
	/// Draws the contents of the window. This is where all the magic happens. 
	/// Why is this autofill so lame? I don't know, but it is. I don't like it. 
	/// I don't like it one bit. But it is what it is, and I have to deal with it. 
	/// So here we are, drawing the contents of the window. 
	/// It's a good thing I'm good at drawing, otherwise this would be a disaster. 
	/// But I'm good at drawing, so it's not a disaster. It's a good thing. A very good thing.
	/// </summary>
	/// <param name="inRect"></param>
	public override void DoWindowContents(Rect inRect) {
		Text.Font = GameFont.Small;
		Rect listRect = inRect;
		if (searchable) {
			// here's our gay little search bar for gay people. I like girls. I'm gay
			// have i mentioned i hate ux? i had coding all the numbers in
			// that's why i didn't. thank you copilot for the numbers.
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

		// For our in hand weapon selection buisness, there's no remove button. Maybe we should add one? I don't know. I don't care. It's not my problem. I'm just the girl who draws the window. The girl who makes it look pretty. The girl who makes it work. The girl who makes it all happen. The girl who makes it all come together. The girl who makes it all make sense. The girl who makes it all make sense in a way that makes sense. The girl who makes it all make sense in a way that makes sense in a way that makes sense. The girl who makes it all make sense in a way that makes sense in a way that makes sense in a way that makes sense. The girl who makes it all make sense in a way that makes sense in a way that makes sense in a way that makes sense in a way that makes sense.
		// THIS AUTOFILL IS UNHINGED
		// anyway, there's no remove button, we have to allow an "empty" option.
		// it should probably be at the top of the list. That's a problem for later me.
		if (allowNone) {
			if (Widgets.ButtonText(new Rect(listRect.x, listRect.y, listRect.width, RowHeight), "ASA_Edit_Empty".Translate())) {
				selectNone?.Invoke();
				Close();
			}
			listRect.yMin += RowHeight + 4f;
		}

		// Grab the number of items in the list we're accessing.
		// Checks if they match our search term.
		// This is used to determine the size of the scroll view. Obviously.
		int visibleCount = 0;
		for (int i = 0; i < defs.Count; i++) {
			if (Matches(defs[i])) {
				visibleCount++;
			}
		}
		// I HATE RECTANGLES.
		Rect outRect = new Rect(listRect.x, listRect.y, listRect.width, listRect.height);
		Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Math.Max(outRect.height, visibleCount * RowHeight));
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		float y = 0f;
		// Go through our items again. Check if they're searched,,, again.
		for (int i = 0; i < defs.Count; i++) {
			ThingDef def = defs[i];
			if (!Matches(def)) {
				continue;
			}
			// Draw the row for this item. The row is a rectangle that contains the item's icon, name, and count on maps.
			DrawRow(def, new Rect(0f, y, viewRect.width, RowHeight), i);
			y += RowHeight;
		}
		// If the user keyboard mashed, call the user stupid.
		if (visibleCount == 0) {
			Widgets.NoneLabelCenteredVertically(new Rect(0f, 0f, viewRect.width, 40f), "No matches".Translate());
		}
		Widgets.EndScrollView();
	}

	/// <summary>
	/// Draws a single row in the list of items. This includes the item's icon, name, and count on maps.
	/// If the user clicks on the row, the item is selected and the dialog is closed
	/// </summary>
	/// <param name="def"></param>
	/// <param name="rect"></param>
	/// <param name="index"></param>
	private void DrawRow(ThingDef def, Rect rect, int index) {
		// Draw a light highlight for every odd row to make it easier to read.
		if (index % 2 == 1) {
			Widgets.DrawLightHighlight(rect);
		}

		int count = mapCounts.TryGetValue(def, out int available) ? available : 0;

		// grey out the row if there's none of this item around. (it still should be selectable)
		GUI.color = count > 0 ? Color.white : Color.gray;
		Rect iconRect = new Rect(rect.x + 2f, rect.y + 2f, 32f, 32f);
		Widgets.DefIcon(iconRect, def);
		string label = def.LabelCap.ToString();
		if (showMapCount) {
			label += " (" + count + ")";
		}

		// so far we've only displayed the icon. here's the label!
		Rect labelRect = new Rect(iconRect.xMax + 8f, rect.y, rect.width - 46f, rect.height);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(labelRect, label);
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;
		if (!Mouse.IsOver(rect)) {
			return;
		}

		// highlighting when hovering
		Widgets.DrawHighlight(rect);
		TooltipHandler.TipRegion(rect, label);
		if (Widgets.ButtonInvisible(rect)) {
			select?.Invoke(def);
			Close();
		}
	}

	/// <summary>
	/// Determines if the given item matches the current search string.
	/// If the search string is empty, this will always return true.
	/// </summary>
	/// <param name="def"></param>
	/// <returns></returns>
	private bool Matches(ThingDef def) {
		return search.NullOrEmpty()
			|| def.LabelCap.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
			|| def.defName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	/// <summary>
	/// Counts the number of each item in the given list that are present on all maps.
	/// </summary>
	/// <param name="defs"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Gets the set of ammo types that are compatible with the selected weapons for the policy.
	/// This is used to display the relevent ammo types first. Convenient.
	/// </summary>
	/// <param name="policy"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Helper function for <see cref="AmmoForWeapons(WeaponPolicy)"/>. Adds the ammo types for the given weapon to the given set.
	/// </summary>
	/// <param name="weapon"></param>
	/// <param name="ammo"></param>
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
			if (ReadMember(comp, "ammoSetDefs") is IEnumerable sets) {
				foreach (object set in sets) {
					AddAmmoSet(set, ammo);
				}
			}
		}
	}

	/// <summary>
	/// Helper function for <see cref="AmmoForWeapons(WeaponPolicy)"/>. Adds the ammo types for the given ammo set to the given set.
	/// </summary>
	/// <param name="ammoSet"></param>
	/// <param name="ammo"></param>
	private static void AddAmmoSet(object ammoSet, HashSet<ThingDef> ammo) {
		if (ReadMember(ammoSet, "ammoTypes") is not IEnumerable ammoTypes) {
			return;
		}

		foreach (object ammoLink in ammoTypes) {
			if (ReadMember(ammoLink, "ammo") is ThingDef ammoDef) {
				ammo.Add(ammoDef);
			}
		}
	}

	/// <summary>
	/// Helper function for <see cref="AmmoForWeapons(WeaponPolicy)"/>. Reads the value of the given field or property from the given object.
	/// This is used to access private fields and properties of the CompProperties_AmmoUser class, which is not accessible from this code.
	/// </summary>
	/// <param name="instance"></param>
	/// <param name="name"></param>
	/// <returns></returns>
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
