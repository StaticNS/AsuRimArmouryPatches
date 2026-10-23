using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AsuRim.Armoury;

public class Gizmo_LoadoutPanel : Gizmo {
	[StaticConstructorOnStartup]
	private static class Glow {
		private const int Size = 64;

		internal static readonly Texture2D Texture = Generate();

		private static Texture2D Generate() {
			Texture2D texture2D = new Texture2D(64, 64, TextureFormat.ARGB32, mipChain: false);
			texture2D.wrapMode = TextureWrapMode.Clamp;
			Vector2 b = new Vector2(31.5f, 31.5f);
			float magnitude = b.magnitude;
			for (int i = 0; i < 64; i++) {
				for (int j = 0; j < 64; j++) {
					float num = Vector2.Distance(new Vector2(j, i), b) / magnitude;
					float num2 = Mathf.Clamp01(1f - num);
					num2 *= num2;
					texture2D.SetPixel(j, i, new Color(1f, 1f, 1f, num2));
				}
			}
			texture2D.Apply();
			return texture2D;
		}
	}

	private struct Slot {
		public ThingDef def;

		public ThingDef stuff;

		public ThingWithComps thing;

		public ThingDef offDef;

		public ThingDef offStuff;

		public ThingWithComps offThing;

		public bool isPair;

		public bool isUnarmed;
	}

	private enum Tip {
		Loadout,
		WalkFire,
		Stance,
		Overwatch,
		ReArm,
		Collapse
	}

	private const float MinWidth = 210f;

	private const float MaxWidth = 360f;

	private const float ButtonSize = 26f;

	private const float Pad = 6f;

	private const float ItemSizeMax = 33f;

	private const float ItemSizeMin = 20f;

	private const float CheckSize = 9f;

	private readonly Pawn pawn;

	private readonly CompArsenal comp;

	private List<Slot> slots;

	private readonly List<Rect> cellBuffer = new List<Rect>();

	private static readonly Color MissingTint = new Color(1f, 1f, 1f, 0.3f);

	private static readonly ConditionalWeakTable<Pawn, Gizmo_LoadoutPanel> panels = new ConditionalWeakTable<Pawn, Gizmo_LoadoutPanel>();

	private int signature;

	private readonly Func<string> tooltipGetter;

	private readonly Action openLoadout;

	private readonly Action toggleWalkFire;

	private readonly Action openStances;

	private readonly Action openOverwatch;

	private readonly Action collapse;

	private readonly Action reArm;

	private static bool ShowsWalkFire => ArmourySettings.walkFireEnabled;

	private bool ShowsStance {
		get {
			if (ArmourySettings.stancesEnabled) {
				return ArmouryStances.HasStances(pawn);
			}
			return false;
		}
	}

	private static bool ShowsOverwatch => ArmourySettings.stancesEnabled;

	private int ButtonCount => 3 + (ShowsWalkFire ? 1 : 0) + (ShowsStance ? 1 : 0) + (ShowsOverwatch ? 1 : 0);

	private List<Slot> Slots {
		get {
			if (slots != null) {
				return slots;
			}
			slots = new List<Slot>();
			List<ThingWithComps> list = new List<ThingWithComps>();
			ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
			ThingWithComps thingWithComps2 = Arsenal.OffHand(pawn);
			if (thingWithComps != null) {
				list.Add(thingWithComps);
			}
			if (thingWithComps2 != null && thingWithComps2 != thingWithComps) {
				list.Add(thingWithComps2);
			}
			List<ThingWithComps> list2 = new List<ThingWithComps>();
			Arsenal.CollectAvailable(pawn, list2, includeHeld: false);
			list.AddRange(list2);
			HashSet<ThingWithComps> claimed = new HashSet<ThingWithComps>();
			WeaponPolicy policy = comp.policy;
			Dictionary<ThingDef, int> covered = new Dictionary<ThingDef, int>();
			ThingDef thingDef = null;
			ThingDef stuff = null;
			ThingDef thingDef2 = null;
			ThingDef thingDef3 = null;
			bool dualWieldEnabled = ArmourySettings.dualWieldEnabled;
			if (dualWieldEnabled && policy != null && policy.mainDef != null && policy.offDef != null) {
				thingDef = policy.mainDef;
				stuff = policy.mainStuff;
				thingDef2 = policy.offDef;
				thingDef3 = policy.offStuff;
			} else if (dualWieldEnabled && thingWithComps != null && thingWithComps2 != null) {
				thingDef = thingWithComps.def;
				stuff = thingWithComps.Stuff;
				thingDef2 = thingWithComps2.def;
				thingDef3 = thingWithComps2.Stuff;
			}
			if (thingDef != null) {
				Slot item = Fill(thingDef, stuff, list, claimed, thingWithComps);
				Slot slot = Fill(thingDef2, thingDef3, list, claimed, thingWithComps2);
				item.offDef = thingDef2;
				item.offStuff = thingDef3;
				item.offThing = slot.thing;
				item.isPair = true;
				slots.Add(item);
				Cover(thingDef);
				Cover(thingDef2);
			} else if (policy?.mainDef != null) {
				slots.Add(Fill(policy.mainDef, policy.mainStuff, list, claimed, thingWithComps));
				Cover(policy.mainDef);
			}
			if (policy != null) {
				for (int i = 0; i < policy.sidearms.Count; i++) {
					LoadoutSlot loadoutSlot = policy.sidearms[i];
					if (loadoutSlot?.def != null) {
						slots.Add(Fill(loadoutSlot.def, loadoutSlot.stuff, list, claimed, null));
						Cover(loadoutSlot.def);
					}
				}
			}
			for (int j = 0; j < comp.stowed.Count; j++) {
				LoadoutSlot loadoutSlot2 = comp.stowed[j];
				if (loadoutSlot2?.def != null && !Uncover(loadoutSlot2.def)) {
					slots.Add(Fill(loadoutSlot2.def, loadoutSlot2.stuff, list, claimed, null));
				}
			}
			AppendUnclaimedHeld(thingWithComps, claimed);
			AppendUnclaimedHeld(thingWithComps2, claimed);
			slots.Add(new Slot {
				isUnarmed = true
			});
			return slots;
			void Cover(ThingDef d) {
				if (d != null) {
					covered.TryGetValue(d, out var value);
					covered[d] = value + 1;
				}
			}
			bool Uncover(ThingDef d) {
				if (d != null && covered.TryGetValue(d, out var value) && value > 0) {
					covered[d] = value - 1;
					return true;
				}
				return false;
			}
		}
	}

	public static Gizmo_LoadoutPanel For(Pawn pawn, CompArsenal comp) {
		if (!panels.TryGetValue(pawn, out var value) || value.comp != comp) {
			value = new Gizmo_LoadoutPanel(pawn, comp);
			panels.AddOrUpdate(pawn, value);
		}
		value.RefreshIfChanged();
		return value;
	}

	public Gizmo_LoadoutPanel(Pawn pawn, CompArsenal comp) {
		this.pawn = pawn;
		this.comp = comp;
		Order = -1000f;
		signature = Signature();
		tooltipGetter = Tooltip;
		openLoadout = () => {
			Find.WindowStack.Add(new FloatMenu(LoadoutGizmo.MenuFor(pawn, comp)));
		};
		toggleWalkFire = ToggleWalkFire;
		openStances = () => {
			Find.WindowStack.Add(new FloatMenu(StanceGizmo.Options(pawn, comp)));
		};
		openOverwatch = () => {
			Find.WindowStack.Add(new FloatMenu(OverwatchGizmo.Options(pawn, comp)));
		};
		reArm = () => ReArm.Order(pawn);
		collapse = () => {
			ArmourySettings.loadoutPanel = false;
			SoundDefOf.Click.PlayOneShotOnCamera();
		};
	}

	private void RefreshIfChanged() {
		int num = Signature();
		if (num != signature) {
			signature = num;
			slots = null;
		}
	}

	private int Signature() {
		HashCode hashCode = default;
		hashCode.Add(ArmourySettings.dualWieldEnabled);
		hashCode.Add(Arsenal.MainHand(pawn));
		hashCode.Add(Arsenal.OffHand(pawn));
		WeaponPolicy policy = comp.policy;
		hashCode.Add(policy);
		if (policy != null) {
			hashCode.Add(policy.mainDef);
			hashCode.Add(policy.mainStuff);
			hashCode.Add(policy.offDef);
			hashCode.Add(policy.offStuff);
			hashCode.Add(policy.sidearms.Count);
			for (int i = 0; i < policy.sidearms.Count; i++) {
				hashCode.Add(policy.sidearms[i]?.def);
				hashCode.Add(policy.sidearms[i]?.stuff);
			}
		}
		hashCode.Add(comp.stowed.Count);
		for (int j = 0; j < comp.stowed.Count; j++) {
			hashCode.Add(comp.stowed[j]?.def);
			hashCode.Add(comp.stowed[j]?.stuff);
		}
		ThingOwner<Thing> thingOwner = pawn.inventory?.innerContainer;
		if (thingOwner != null) {
			for (int k = 0; k < thingOwner.Count; k++) {
				if (thingOwner[k] is ThingWithComps thingWithComps && ArsenalRules.IsWieldable(thingWithComps)) {
					hashCode.Add(thingWithComps);
				}
			}
		}
		return hashCode.ToHashCode();
	}

	private void ToggleWalkFire() {
		bool walkFire = !comp.walkFire;
		List<Pawn> list = Commanded.With(pawn);
		for (int i = 0; i < list.Count; i++) {
			CompArsenal compArsenal = CompArsenal.For(list[i]);
			if (compArsenal != null) {
				compArsenal.walkFire = walkFire;
			}
		}
		SoundDefOf.Click.PlayOneShotOnCamera();
	}

	private string TipText(Tip tip) {
		switch (tip) {
			case Tip.Loadout:
				return LoadoutTooltip();
			case Tip.WalkFire:
				return ArmouryText.Get("ASA_WalkFire") + "\n\n" + ArmouryText.Get("ASA_WalkFire_Desc");
			case Tip.Stance:
				return comp.stance.Label() + "\n\n" + comp.stance.Desc();
			case Tip.Overwatch: {
					Area overwatchArea = comp.OverwatchArea;
					if (!comp.overwatch || overwatchArea == null) {
						return ArmouryText.Get("ASA_Overwatch_Desc");
					}
					return "ASA_Overwatch_ArmedDesc".Translate(overwatchArea.Label);
				}
			case Tip.ReArm:
				return ArmouryText.Get("ASA_Rearm") + "\n\n" + ArmouryText.Get("ASA_Rearm_Desc");
			default:
				return ArmouryText.Get("ASA_Panel_Collapse");
		}
	}

	public override bool GroupsWith(Gizmo other) {
		return other is Gizmo_LoadoutPanel;
	}

	private void AppendUnclaimedHeld(ThingWithComps held, HashSet<ThingWithComps> claimed) {
		if (held != null && !claimed.Contains(held)) {
			claimed.Add(held);
			slots.Add(new Slot {
				def = held.def,
				stuff = held.Stuff,
				thing = held
			});
		}
	}

	private static Slot Fill(ThingDef def, ThingDef stuff, List<ThingWithComps> carried, HashSet<ThingWithComps> claimed, ThingWithComps preferred) {
		Slot result = new Slot {
			def = def,
			stuff = stuff
		};
		if (preferred != null && preferred.def == def && !claimed.Contains(preferred)) {
			claimed.Add(preferred);
			result.thing = preferred;
			return result;
		}
		ThingWithComps thingWithComps = null;
		for (int i = 0; i < carried.Count; i++) {
			ThingWithComps thingWithComps2 = carried[i];
			if (thingWithComps2.def == def && !claimed.Contains(thingWithComps2)) {
				if (thingWithComps2.Stuff == stuff) {
					claimed.Add(thingWithComps2);
					result.thing = thingWithComps2;
					return result;
				}
				if (thingWithComps == null) {
					thingWithComps = thingWithComps2;
				}
			}
		}
		if (thingWithComps != null) {
			claimed.Add(thingWithComps);
			result.thing = thingWithComps;
		}
		return result;
	}

	public override float GetWidth(float maxWidth) {
		float b = 12f + (float)Slots.Count * 35f;
		float buttonWidth = 32f + (float)ButtonCount * 26f;
		return Mathf.Clamp(Mathf.Max(210f, b, buttonWidth), 210f, Mathf.Min(360f, maxWidth));
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms) {
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect rect2 = rect.ContractedBy(6f);
		bool flag = Mouse.IsOver(rect2);
		Material material = (parms.lowLight ? TexUI.GrayscaleGUI : null);
		GUI.color = (parms.lowLight ? Command.LowLightBgColor : Color.white);
		GenUI.DrawTextureWithMaterial(rect, parms.shrunk ? Command.BGTexShrunk : Command.BGTex, material);
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
		DrawLabel(rect2);
		bool flag2 = DrawButtons(rect);
		DrawItems(rect2);
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;
		if (Find.WindowStack.FloatMenu == null && !flag2) {
			TooltipHandler.TipRegion(rect, tooltipGetter, 2545873);
		}
		return new GizmoResult(flag ? GizmoState.Mouseover : GizmoState.Clear);
	}

	private void DrawLabel(Rect inner) {
		string text = (comp.policy != null) ? comp.policy.label : ArmouryText.Get("ASA_Policy_None");
		float num = inner.width - (float)ButtonCount * 26f - 4f;
		if (num < 20f) {
			num = 20f;
		}
		Rect rect = inner;
		rect.width = num;
		rect.height = Text.CalcSize(text).y;
		Widgets.Label(rect, text.Truncate(num));
		if (comp.policy == null || !Mouse.IsOver(rect)) {
			return;
		}
		Widgets.DrawHighlight(rect);
		if (!Widgets.ButtonInvisible(rect)) {
			return;
		}
		WeaponPolicy policy = comp.policy;
		Map map = pawn.Map;
		if (map == null) {
			return;
		}
		Find.Selector.ClearSelection();
		foreach (Pawn item in map.mapPawns.FreeColonistsSpawned) {
			if (CompArsenal.For(item)?.policy == policy) {
				Find.Selector.Select(item, playSound: false);
			}
		}
	}

	private bool DrawButtons(Rect rect) {
		bool flag = false;
		float y = rect.y + 6f;
		float num = rect.x + rect.width - 6f;
		float num2 = 1f;
		flag |= DrawButton(new Rect(num - 26f * num2++, y, 26f, 26f), StanceIcons.Get("UI/Stances/ASA_Loadout", TexCommand.ForbidOff), Tip.Loadout, openLoadout);
		if (ShowsWalkFire) {
			flag |= DrawButton(new Rect(num - 26f * num2++, y, 26f, 26f), StanceIcons.Get("UI/Stances/ASA_WalkFire", TexCommand.Attack), Tip.WalkFire, toggleWalkFire, comp.walkFire);
		}
		if (ShowsStance) {
			flag |= DrawButton(new Rect(num - 26f * num2++, y, 26f, 26f), StanceIcons.Get(comp.stance), Tip.Stance, openStances);
		}
		if (ShowsOverwatch) {
			bool armed = comp.overwatch && comp.OverwatchArea != null;
			flag |= DrawButton(new Rect(num - 26f * num2++, y, 26f, 26f), PostGizmos.OverwatchIcon(armed), Tip.Overwatch, openOverwatch);
		}

		flag |= DrawButton(new Rect(num - 26f * num2++, y, 26f, 26f), StanceIcons.Get("UI/Stances/ASA_Rearm", TexCommand.RearmTrap), Tip.ReArm, reArm);

		return flag | DrawButton(new Rect(num - 26f * num2, y, 26f, 26f), TexButton.Minus, Tip.Collapse, collapse);
	}

	private bool DrawButton(Rect rect, Texture2D icon, Tip tip, Action action, bool? toggle = null) {
		Widgets.DrawTextureFitted(rect, icon, 1f);
		if (toggle.HasValue) {
			GUI.DrawTexture(new Rect(rect.xMax - 9f, rect.y, 9f, 9f), toggle.Value ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);
		}
		if (!Mouse.IsOver(rect)) {
			return false;
		}
		Widgets.DrawHighlight(rect);
		TooltipHandler.TipRegion(rect, TipText(tip));
		MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
		if (Widgets.ButtonInvisible(rect)) {
			action();
		}
		return true;
	}

	private void DrawItems(Rect inner) {
		Rect rect = new Rect(inner.x, inner.y + 26f + 4f, inner.width, inner.height - 26f - 4f);
		List<Slot> list = Slots;
		if (list.Count == 0) {
			GUI.color = ColoredText.SubtleGrayColor;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, "(" + "ASA_Panel_NoItems".Translate() + ")");
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			return;
		}
		float num = Mathf.Min(33f, rect.height);
		int num2 = list.Count;
		if ((float)num2 * (num + 2f) > rect.width) {
			num = Mathf.Max(20f, Mathf.Min(num, rect.width / (float)num2 - 2f));
			num2 = Mathf.Max(1, Mathf.FloorToInt(rect.width / (num + 2f)));
		}
		ThingWithComps main = Arsenal.MainHand(pawn);
		ThingWithComps off = Arsenal.OffHand(pawn);
		cellBuffer.Clear();
		float num3 = rect.x;
		for (int i = 0; i < num2 && i < list.Count; i++) {
			cellBuffer.Add(new Rect(num3, rect.y + (rect.height - num) / 2f, num, num));
			num3 += num + 2f;
		}
		DrawHeldGlow(list, cellBuffer, main, off);
		for (int j = 0; j < cellBuffer.Count; j++) {
			DrawSlot(cellBuffer[j], list[j], main, off);
		}
		int num4 = list.Count - num2;
		if (num4 > 0) {
			Rect rect2 = new Rect(num3, rect.y, rect.width - (num3 - rect.x), rect.height);
			Text.Anchor = TextAnchor.MiddleLeft;
			GUI.color = ColoredText.SubtleGrayColor;
			Widgets.Label(rect2, "+" + num4);
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
	}

	private void DrawHeldGlow(List<Slot> list, List<Rect> cells, ThingWithComps main, ThingWithComps off) {
		int num = -1;
		for (int i = 0; i <= cells.Count; i++) {
			bool flag = i < cells.Count && IsHeld(list[i], main, off);
			if (flag && num < 0) {
				num = i;
			} else {
				if (flag || num < 0) {
					continue;
				}
				Rect rect = cells[num];
				Rect rect2 = cells[i - 1];
				Rect rect3 = new Rect(rect.x, rect.y, rect2.xMax - rect.x, rect.height);
				float margin = rect3.height * 0.35f;
				Rect position = rect3.ExpandedBy(margin);
				bool flag2 = false;
				for (int j = num; j < i; j++) {
					if (flag2) {
						break;
					}
					flag2 = IsForced(list[j], main, off);
				}
				GUI.color = (flag2 ? new Color(1f, 0.82f, 0.4f, 0.65f) : new Color(1f, 1f, 1f, 0.5f));
				GUI.DrawTexture(position, Glow.Texture);
				GUI.color = Color.white;
				num = -1;
			}
		}
	}

	private bool IsHeld(Slot slot, ThingWithComps main, ThingWithComps off) {
		if (slot.isUnarmed) {
			return comp.forcedUnarmed;
		}
		if (slot.thing == null) {
			return false;
		}
		if (slot.thing == main || slot.thing == off) {
			return true;
		}
		if (slot.isPair && slot.offThing != null) {
			if (slot.offThing != main) {
				return slot.offThing == off;
			}
			return true;
		}
		return false;
	}

	private static void DrawHalf(Rect rect, ThingDef def, ThingDef stuff, ThingWithComps thing) {
		if (thing != null) {
			UniqueWeaponVisuals.DrawGlow(rect, thing);
			Widgets.ThingIcon(rect, thing);
		} else if (def != null) {
			GUI.color = MissingTint;
			Widgets.DrawTextureFitted(rect, Widgets.GetIconFor(def, stuff), 1f);
			GUI.color = Color.white;
		}
	}

	private bool IsForced(Slot slot, ThingWithComps main, ThingWithComps off) {
		if (comp.forced == null) {
			return false;
		}
		if (comp.forced == slot.thing || comp.forced == slot.offThing) {
			return true;
		}
		if (IsHeld(slot, main, off)) {
			if (comp.forced != main) {
				return comp.forced == off;
			}
			return true;
		}
		return false;
	}

	private void DrawSlot(Rect cell, Slot slot, ThingWithComps main, ThingWithComps off) {
		if (slot.isUnarmed) {
			Widgets.DrawTextureFitted(cell, TexCommand.AttackMelee, 1f);
			if (Mouse.IsOver(cell)) {
				Widgets.DrawHighlight(cell);
				MouseoverSounds.DoRegion(cell, SoundDefOf.Mouseover_Command);
				TooltipHandler.TipRegion(cell, ArmouryText.Get("ASA_Loadout_FightUnarmed"));
				if (!comp.forcedUnarmed && Widgets.ButtonInvisible(cell)) {
					ArsenalActions.GoUnarmed(pawn);
					comp.forced = null;
					comp.forcedUnarmed = true;
				}
			}
			return;
		}
		if (slot.isPair) {
			PairIcon.Rects(cell, out var back, out var front);
			DrawHalf(back, slot.offDef, slot.offStuff, slot.offThing);
			DrawHalf(front, slot.def, slot.stuff, slot.thing);
		} else if (slot.thing != null) {
			UniqueWeaponVisuals.DrawGlow(cell, slot.thing);
			Widgets.ThingIcon(cell, slot.thing);
		} else if (slot.def != null) {
			GUI.color = MissingTint;
			Widgets.DrawTextureFitted(cell, Widgets.GetIconFor(slot.def, slot.stuff), 1f);
			GUI.color = Color.white;
		}
		if (!Mouse.IsOver(cell)) {
			return;
		}
		Widgets.DrawHighlight(cell);
		MouseoverSounds.DoRegion(cell, SoundDefOf.Mouseover_Command);
		TooltipHandler.TipRegion(cell, SlotTooltip(slot, main, off));
		if (Event.current.type == EventType.mouseDown && Event.current.button == 1 && Mouse.IsOver(cell)) {
			Event.current.Use();
			if (slot.thing != null) {
				if (slot.isPair && slot.offThing != null) {
					Drop(slot.offThing);
				}
				Drop(slot.thing);
			} else if (slot.def != null) {
				RemoveGhost(slot);
			}
		} else {
			if (slot.thing == null) {
				return;
			}
			if (Event.current.type == EventType.mouseDown && Event.current.button == 0 && Event.current.clickCount == 2 && Mouse.IsOver(cell)) {
				Event.current.Use();
				if (IsForced(slot, main, off)) {
					comp.forced = null;
					return;
				}
				comp.forcedUnarmed = false;
				Equip(slot);
				comp.forced = Arsenal.MainHand(pawn);
			} else if (!IsHeld(slot, main, off) && Widgets.ButtonInvisible(cell)) {
				comp.forced = null;
				comp.forcedUnarmed = false;
				Equip(slot);
			}
		}
	}

	private void Equip(Slot slot) {
		if (slot.isPair && comp.policy != null) {
			PolicyApplier.Apply(pawn, comp.policy, applyStance: false);
		} else {
			ArsenalActions.EquipMain(pawn, slot.thing, dropCurrent: false, manual: true);
		}
	}

	private string SlotTooltip(Slot slot, ThingWithComps main, ThingWithComps off) {
		if (slot.isPair) {
			string text = ((comp.policy != null) ? comp.policy.Summary : slot.def.label);
			if (slot.thing == null || slot.offThing == null) {
				return "ASA_Panel_SlotEmpty".Translate(text.CapitalizeFirst()) + "\n\n" + "ASA_Panel_ActionsHint".Translate();
			}
			if (IsHeld(slot, main, off)) {
				return text.CapitalizeFirst() + "\n\n" + "ASA_Panel_ActionsHint".Translate();
			}
			return "ASA_Loadout_SwapTo".Translate(text) + "\n\n" + "ASA_Panel_ActionsHint".Translate();
		}
		if (slot.thing == null) {
			return "ASA_Panel_SlotEmpty".Translate(slot.def.LabelCap) + "\n\n" + "ASA_Panel_ActionsHint".Translate();
		}
		if (IsForced(slot, main, off)) {
			return slot.thing.LabelCap + "\n\n" + "ASA_Panel_Forced".Translate();
		}
		if (slot.thing == main) {
			return slot.thing.LabelCap + "\n\n" + "ASA_Panel_ActionsHint".Translate();
		}
		return "ASA_Loadout_SwapTo".Translate(slot.thing.LabelCap) + "\n\n" + "ASA_Panel_ActionsHint".Translate();
	}

	private void Drop(ThingWithComps weapon) {
		if (weapon == Arsenal.MainHand(pawn)) {
			ArsenalActions.StowMainHand(pawn, drop: true);
			return;
		}
		if (weapon == Arsenal.OffHand(pawn)) {
			ArsenalActions.StowOffHand(pawn, drop: true);
			return;
		}
		ArsenalActions.Forget(pawn, weapon);
		pawn.inventory?.innerContainer.TryDrop(weapon, pawn.Position, pawn.Map, ThingPlaceMode.Near, out var _);
	}

	private void RemoveGhost(Slot slot) {
		WeaponPolicy policy = comp.policy;
		if (policy != null) {
			bool flag = false;
			if (policy.mainDef == slot.def && policy.mainStuff == slot.stuff) {
				policy.mainDef = null;
				policy.mainStuff = null;
				flag = true;
			}
			if (slot.isPair && policy.offDef == slot.offDef && policy.offStuff == slot.offStuff) {
				policy.offDef = null;
				policy.offStuff = null;
				flag = true;
			}
			if (flag) {
				return;
			}
			int num = policy.sidearms.FindIndex((LoadoutSlot s) => s != null && s.def == slot.def && s.stuff == slot.stuff);
			if (num >= 0) {
				policy.sidearms.RemoveAt(num);
				return;
			}
		}
		int num2 = comp.stowed.FindIndex((LoadoutSlot s) => s != null && s.def == slot.def && s.stuff == slot.stuff);
		if (num2 >= 0) {
			comp.stowed.RemoveAt(num2);
		}
	}

	private string LoadoutTooltip() {
		if (comp.policy == null) {
			return ArmouryText.Get("ASA_Policy_NoneDesc");
		}
		return comp.policy.label + "\n\n" + comp.policy.Summary;
	}

	private string Tooltip() {
		string text = ((comp.policy != null) ? comp.policy.label : ((string)"ASA_Policy_None".Translate())).Colorize(ColoredText.TipSectionTitleColor) + "\n\n";
		if (ShowsStance) {
			text += (comp.stance.LabelKey().Translate() + "\n" + comp.stance.DescKey().Translate()).Resolve();
		}
		List<Slot> list = Slots;
		if (list.Count > 0) {
			List<string> list2 = new List<string>();
			for (int i = 0; i < list.Count; i++) {
				Slot slot = list[i];
				if (!slot.isUnarmed) {
					list2.Add((slot.thing != null) ? slot.thing.LabelCap : ((string)"ASA_Panel_SlotEmpty".Translate(slot.def.LabelCap)));
				}
			}
			text = text + "\n\n" + "ASA_Panel_Carrying".Translate().Colorize(ColoredText.TipSectionTitleColor) + "\n" + list2.ToLineList(" - ");
		}
		return text;
	}
}
