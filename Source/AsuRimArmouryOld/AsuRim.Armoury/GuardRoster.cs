using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AsuRim.Armoury;

internal class GuardRoster
{
	private struct Entry
	{
		public Pawn pawn;

		public CompGuardPost post;

		public Pawn charge;
	}

	private const float RowHeight = 34f;

	private Vector2 scrollPosition;

	private float lastHeight = 300f;

	private string search = "";

	private static readonly List<Entry> entries = new List<Entry>();

	private static CombatStance? armedStance;

	private static Pawn armedCharge;

	private static bool armedUnguard;

	private static readonly HashSet<Pawn> dragStanceApplied = new HashSet<Pawn>();

	private static readonly HashSet<Pawn> dragProtectApplied = new HashSet<Pawn>();

	private const int CollectEveryFrames = 10;

	private static readonly List<Entry> collected = new List<Entry>();

	private static int collectedFrame = -9999;

	private static Map collectedMap;

	internal void Draw(Rect inRect)
	{
		EventType type = Event.current.type;
		if (Time.frameCount - collectedFrame >= 10 || collectedMap != Find.CurrentMap || (type != EventType.layout && type != EventType.repaint))
		{
			Collect();
			collectedFrame = Time.frameCount;
			collectedMap = Find.CurrentMap;
		}
		entries.Clear();
		for (int i = 0; i < collected.Count; i++)
		{
			if (collected[i].pawn.Spawned && collected[i].pawn.Map == collectedMap)
			{
				entries.Add(collected[i]);
			}
		}
		if (!Input.GetMouseButton(0))
		{
			dragStanceApplied.Clear();
			dragProtectApplied.Clear();
		}
		Rect rect = new Rect(inRect.x, inRect.y, inRect.width, 26f);
		search = Widgets.TextField(rect, search);
		if (search.NullOrEmpty())
		{
			Text.Anchor = TextAnchor.MiddleLeft;
			GUI.color = new Color(1f, 1f, 1f, 0.4f);
			Widgets.Label(new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height), "ASA_Post_Search".Translate());
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
		else
		{
			entries.RemoveAll((Entry e) => e.pawn.LabelCap.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 && (e.pawn.kindDef?.label == null || e.pawn.kindDef.label.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0));
		}
		Rect rect2 = new Rect(inRect.x, rect.yMax + 4f, inRect.width, 24f);
		Text.Anchor = TextAnchor.MiddleLeft;
		GUI.color = new Color(1f, 1f, 1f, 0.55f);
		Widgets.Label(new Rect(rect2.x + 36f, rect2.y, 180f, rect2.height), "ASA_Guards_Who".Translate());
		Widgets.Label(new Rect(rect2.x + 226f, rect2.y, 220f, rect2.height), "ASA_Guards_Where".Translate());
		Widgets.Label(new Rect(rect2.x + 456f, rect2.y, 160f, rect2.height), "ASA_Guards_Orders".Translate());
		Widgets.Label(new Rect(rect2.x + 626f, rect2.y, 120f, rect2.height), "ASA_Guards_State".Translate());
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		Rect outRect = new Rect(inRect.x, rect2.yMax + 4f, inRect.width, inRect.yMax - (rect2.yMax + 4f));
		Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, lastHeight);
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		float num = 0f;
		for (int num2 = 0; num2 < entries.Count; num2++)
		{
			DrawRow(new Rect(0f, num, viewRect.width, 34f), entries[num2], num2);
			num += 34f;
		}
		if (entries.Count == 0)
		{
			Widgets.NoneLabelCenteredVertically(new Rect(0f, 0f, viewRect.width, 60f), "ASA_Guards_None".Translate());
			num = 60f;
		}
		lastHeight = Mathf.Max(num, 1f);
		Widgets.EndScrollView();
		entries.Clear();
	}

	private static void Collect()
	{
		collected.Clear();
		Map currentMap = Find.CurrentMap;
		if (currentMap == null)
		{
			return;
		}
		GuardRegistry guardRegistry = GuardRegistry.For(currentMap);
		foreach (Pawn item in currentMap.mapPawns.AllPawnsSpawned)
		{
			if (StanceRules.CanTakeOrders(item))
			{
				CompArsenal compArsenal = CompArsenal.For(item);
				if (compArsenal != null)
				{
					CompGuardPost post = guardRegistry?.PostFor(item);
					collected.Add(new Entry
					{
						pawn = item,
						post = post,
						charge = compArsenal.guarding
					});
				}
			}
		}
		collected.Sort((Entry a, Entry b) =>
		{
			int num = Category(a.pawn);
			int num2 = Category(b.pawn);
			return (num == num2) ? string.Compare(a.pawn.LabelShort, b.pawn.LabelShort, StringComparison.OrdinalIgnoreCase) : num.CompareTo(num2);
		});
	}

	private static int Category(Pawn pawn)
	{
		if (pawn.RaceProps.Humanlike)
		{
			return 0;
		}
		if (pawn.RaceProps.IsMechanoid)
		{
			return 1;
		}
		if (pawn.RaceProps.Animal)
		{
			return 2;
		}
		return 3;
	}

	private static void OpenStanceMenu(Pawn pawn, CompArsenal comp)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		foreach (CombatStance value in Enum.GetValues(typeof(CombatStance)))
		{
			if (!value.Offerable() || value.SquadOnly())
			{
				continue;
			}
			CombatStance choice = value;
			Action<Rect> mouseoverGuiAction = (Rect rect) =>
			{
				TooltipHandler.TipRegion(rect, choice.LabelKey().Translate() + "\n\n" + choice.DescKey().Translate());
			};
			if (!StanceRules.Allows(pawn, choice, out var reason))
			{
				list.Add(new FloatMenuOption(choice.LabelKey().Translate() + " (" + reason + ")", null, StanceIcons.Get(choice), Color.white, MenuOptionPriority.Default, mouseoverGuiAction));
				continue;
			}
			list.Add(new FloatMenuOption(choice.LabelKey().Translate(), () =>
			{
				StanceGizmo.SetStance(pawn, comp, choice, manual: true);
				armedStance = choice;
				dragStanceApplied.Add(pawn);
			}, StanceIcons.Get(choice), Color.white, MenuOptionPriority.Default, mouseoverGuiAction));
		}
		Find.WindowStack.Add(new FloatMenu(list));
	}

	private static void OpenProtectMenu(Pawn guard)
	{
		CompArsenal comp = CompArsenal.For(guard);
		if (comp == null)
		{
			return;
		}
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		if (comp.guarding != null)
		{
			list.Add(new FloatMenuOption("ASA_Guards_Unguard".Translate(), () =>
			{
				comp.guarding = null;
				guard.jobs?.EndCurrentJob(JobCondition.InterruptForced);
				armedCharge = null;
				armedUnguard = true;
				dragProtectApplied.Add(guard);
			}));
		}
		Map map = guard.Map;
		if (map != null)
		{
			foreach (Pawn item in map.mapPawns.FreeColonistsSpawned)
			{
				if (item != guard)
				{
					Pawn captured = item;
					list.Add(new FloatMenuOption(captured.LabelShortCap, () =>
					{
						comp.guarding = captured;
						guard.jobs?.EndCurrentJob(JobCondition.InterruptForced);
						armedCharge = captured;
						armedUnguard = false;
						dragProtectApplied.Add(guard);
					}));
				}
			}
		}
		if (list.Count == 0)
		{
			list.Add(new FloatMenuOption("ASA_Guards_NobodyToGuard".Translate(), null));
		}
		Find.WindowStack.Add(new FloatMenu(list));
	}

	private void DrawRow(Rect row, Entry entry, int index)
	{
		if (index % 2 == 1)
		{
			Widgets.DrawLightHighlight(row);
		}
		Widgets.DrawHighlightIfMouseover(row);
		Widgets.ThingIcon(new Rect(row.x, row.y + 1f, 32f, 32f), entry.pawn);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(new Rect(row.x + 36f, row.y, 186f, row.height), entry.pawn.LabelShortCap);
		string label;
		if (entry.charge == null)
		{
			if (entry.post == null)
			{
				label = "ASA_Guards_Unassigned".Translate();
			}
			else if (entry.post.Kind != PostKind.Mech)
			{
				label = ((entry.post.Kind == PostKind.Patrol) ? ((string)"ASA_Guards_Route".Translate(entry.post.group.ToString())) : ((string)"ASA_Guards_Post".Translate(entry.post.group.ToString())));
			}
			else
			{
				label = "ASA_Guards_MechPost".Translate();
			}
		}
		else
		{
			Squad squad = SquadManager.Current?.SquadOf(entry.pawn);
			label = ((squad != null && squad.Escorting && squad.Leader == entry.charge) ? ((string)"ASA_Guards_Escorting".Translate(entry.charge.LabelShortCap, squad.label)) : ((string)"ASA_Guards_Guarding".Translate(entry.charge.LabelShortCap)));
		}
		Widgets.Label(new Rect(row.x + 226f, row.y, 226f, row.height), label);
		CompArsenal compArsenal = CompArsenal.For(entry.pawn);
		CombatStance combatStance = compArsenal?.EffectiveStance ?? CombatStance.Hold;
		Rect rect = new Rect(row.x + 456f, row.y + 3f, 166f, row.height - 6f);
		if (compArsenal != null && Input.GetMouseButtonDown(0) && Mouse.IsOver(rect))
		{
			armedStance = combatStance;
			dragStanceApplied.Clear();
			dragStanceApplied.Add(entry.pawn);
		}
		string reason;
		if (compArsenal == null)
		{
			Widgets.Label(rect, combatStance.LabelKey().Translate());
		}
		else if (Widgets.ButtonText(rect, combatStance.LabelKey().Translate()))
		{
			OpenStanceMenu(entry.pawn, compArsenal);
		}
		else if (armedStance.HasValue && Input.GetMouseButton(0) && Mouse.IsOver(rect) && dragStanceApplied.Add(entry.pawn) && StanceRules.Allows(entry.pawn, armedStance.Value, out reason))
		{
			StanceGizmo.SetStance(entry.pawn, compArsenal, armedStance.Value, manual: true);
			SoundDefOf.Click.PlayOneShotOnCamera();
		}
		Text.Anchor = TextAnchor.MiddleLeft;
		string label2 = "-";
		if (compArsenal != null && compArsenal.OnDuty)
		{
			label2 = "ASA_Guards_OnStation".Translate();
		}
		else if (entry.post != null || entry.charge != null)
		{
			label2 = "ASA_Guards_EnRoute".Translate();
		}
		Widgets.Label(new Rect(row.x + 626f, row.y, 110f, row.height), label2);
		Text.Anchor = TextAnchor.UpperLeft;
		Rect rect2 = new Rect(row.xMax - 150f, row.y + 3f, 146f, row.height - 6f);
		if (compArsenal != null && Input.GetMouseButtonDown(0) && Mouse.IsOver(rect2))
		{
			armedCharge = entry.charge;
			armedUnguard = entry.charge == null;
			dragProtectApplied.Clear();
			dragProtectApplied.Add(entry.pawn);
		}
		if (Widgets.ButtonText(rect2, (entry.charge != null) ? ((string)"ASA_Guards_Unguard".Translate()) : ((string)"ASA_Guards_Protect".Translate())))
		{
			OpenProtectMenu(entry.pawn);
		}
		else if (compArsenal != null && (armedCharge != null || armedUnguard) && Input.GetMouseButton(0) && Mouse.IsOver(rect2) && dragProtectApplied.Add(entry.pawn))
		{
			if (armedUnguard)
			{
				compArsenal.guarding = null;
				entry.pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			else if (Bodyguard.CanGuard(entry.pawn, armedCharge))
			{
				compArsenal.guarding = armedCharge;
				entry.pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
		}
		Rect butRect = row;
		butRect.xMax = rect.xMin - 4f;
		if (Widgets.ButtonInvisible(butRect))
		{
			CameraJumper.TryJumpAndSelect(entry.pawn);
		}
	}
}
