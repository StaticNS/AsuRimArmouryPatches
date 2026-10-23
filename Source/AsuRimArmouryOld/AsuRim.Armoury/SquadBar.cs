using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AsuRimArmoury.Kit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AsuRim.Armoury;

public static class SquadBar
{
	public const int MaxNameLength = 56;

	private const int MaxPortraits = 6;

	private const float MinPortraitWidth = 17f;

	private static readonly List<Pawn> ordered;

	private static readonly Dictionary<Squad, (Pawn first, Pawn second)> anchors;

	private static readonly FieldInfo moodBGTexField = AccessTools.Field(typeof(ColonistBarColonistDrawer), "MoodBGTex");

	private static readonly MethodInfo drawSelectionOverlayMethod = AccessTools.Method(typeof(ColonistBarColonistDrawer), "DrawSelectionOverlayOnGUI");

	static SquadBar()
	{
		ordered = new List<Pawn>();
		anchors = new Dictionary<Squad, (Pawn, Pawn)>();
		CacheReset.Register(anchors.Clear);
	}

	internal static void RecacheAnchors(List<ColonistBar.Entry> entries, List<Squad> squads)
	{
		anchors.Clear();
		if (squads == null)
		{
			return;
		}
		for (int i = 0; i < squads.Count; i++)
		{
			Squad squad = squads[i];
			if (squad.collapsed)
			{
				Anchors(squad, entries, out var first, out var second);
				if (first != null)
				{
					anchors[squad] = (first, second);
				}
			}
		}
	}

	private static void CachedAnchors(Squad squad, out Pawn first, out Pawn second)
	{
		List<ColonistBar.Entry> entries = Find.ColonistBar.Entries;
		if (anchors.TryGetValue(squad, out (Pawn, Pawn) value))
		{
			(first, second) = value;
		}
		else if (!squad.collapsed)
		{
			Anchors(squad, entries, out first, out second);
		}
		else
		{
			first = null;
			second = null;
		}
	}

	public static List<Pawn> Ordered(Squad squad)
	{
		ordered.Clear();
		List<Pawn> members = squad.Members;
		if (squad.Leader != null && members.Contains(squad.Leader))
		{
			ordered.Add(squad.Leader);
		}
		for (int i = 0; i < members.Count; i++)
		{
			if (members[i] != squad.Leader)
			{
				ordered.Add(members[i]);
			}
		}
		return ordered;
	}

	public static bool BarEligible(Pawn pawn)
	{
		if (pawn != null && !pawn.Dead)
		{
			if (!pawn.IsColonist)
			{
				return pawn.IsColonySubhumanPlayerControlled;
			}
			return true;
		}
		return false;
	}

	public static void Anchors(Squad squad, List<ColonistBar.Entry> entries, out Pawn first, out Pawn second)
	{
		first = null;
		second = null;
		for (int i = 0; i < entries.Count; i++)
		{
			Pawn pawn = entries[i].pawn;
			if (pawn != null && squad.Contains(pawn))
			{
				if (first == null)
				{
					first = pawn;
				}
				else if (second == null)
				{
					second = pawn;
					break;
				}
			}
		}
	}

	private static Squad CollapsedSquadOf(Pawn pawn)
	{
		SquadManager current = SquadManager.Current;
		if (current == null || pawn == null)
		{
			return null;
		}
		Squad squad = current.SquadOf(pawn);
		if (squad == null || !squad.collapsed)
		{
			return null;
		}
		return squad;
	}

	public static int CellRole(Pawn pawn, out Squad squad)
	{
		squad = CollapsedSquadOf(pawn);
		if (squad == null)
		{
			return 0;
		}
		CachedAnchors(squad, out var first, out var second);
		if (pawn == first)
		{
			return 1;
		}
		if (pawn == second)
		{
			return 2;
		}
		return 0;
	}

	public static float AverageMood(Squad squad)
	{
		List<Pawn> members = squad.Members;
		float num = 0f;
		int num2 = 0;
		for (int i = 0; i < members.Count; i++)
		{
			Need_Mood need_Mood = members[i].needs?.mood;
			if (need_Mood != null)
			{
				num += need_Mood.CurLevelPercentage;
				num2++;
			}
		}
		if (num2 != 0)
		{
			return num / (float)num2;
		}
		return -1f;
	}

	public static Rect PanelRect(Rect rect, Pawn colonist, Squad squad)
	{
		List<ColonistBar.Entry> entries = Find.ColonistBar.Entries;
		List<Vector2> drawLocs = Find.ColonistBar.DrawLocs;
		CachedAnchors(squad, out var first, out var second);
		if (second == null || colonist != first)
		{
			return rect;
		}
		int num = -1;
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].pawn == first)
			{
				num = i;
				break;
			}
		}
		if (num < 0 || num + 1 >= entries.Count || num + 1 >= drawLocs.Count)
		{
			return rect;
		}
		if (entries[num + 1].pawn != second)
		{
			return rect;
		}
		if (!Mathf.Approximately(drawLocs[num + 1].y, drawLocs[num].y))
		{
			return rect;
		}
		float num2 = drawLocs[num + 1].x + Find.ColonistBar.Size.x;
		return new Rect(rect.x, rect.y, num2 - rect.x, rect.height);
	}

	public static Rect ClickRect(Rect rect, Pawn colonist, Squad squad, int role)
	{
		if (role != 1)
		{
			return rect;
		}
		List<ColonistBar.Entry> entries = Find.ColonistBar.Entries;
		List<Vector2> drawLocs = Find.ColonistBar.DrawLocs;
		CachedAnchors(squad, out var first, out var second);
		if (second == null || colonist != first)
		{
			return rect;
		}
		int num = IndexOf(entries, first);
		if (num < 0 || num + 1 >= entries.Count || num + 1 >= drawLocs.Count)
		{
			return rect;
		}
		if (entries[num + 1].pawn != second)
		{
			return rect;
		}
		if (!Mathf.Approximately(drawLocs[num + 1].y, drawLocs[num].y))
		{
			return rect;
		}
		return new Rect(rect.x, rect.y, drawLocs[num + 1].x - rect.x, rect.height);
	}

	public static int IndexOf(List<ColonistBar.Entry> entries, Pawn pawn)
	{
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].pawn == pawn)
			{
				return i;
			}
		}
		return -1;
	}

	public static bool TryPanelRect(Squad squad, out Rect panel, out Pawn anchor)
	{
		panel = default;
		anchor = null;
		if (!squad.collapsed)
		{
			return false;
		}
		List<ColonistBar.Entry> entries = Find.ColonistBar.Entries;
		List<Vector2> drawLocs = Find.ColonistBar.DrawLocs;
		CachedAnchors(squad, out var first, out var second);
		if (first == null)
		{
			return false;
		}
		int num = IndexOf(entries, first);
		if (num < 0 || num >= drawLocs.Count)
		{
			return false;
		}
		anchor = first;
		Vector2 size = Find.ColonistBar.Size;
		panel = new Rect(drawLocs[num].x, drawLocs[num].y, size.x, size.y);
		if (second != null && num + 1 < entries.Count && num + 1 < drawLocs.Count && entries[num + 1].pawn == second && Mathf.Approximately(drawLocs[num + 1].y, drawLocs[num].y))
		{
			panel.xMax = drawLocs[num + 1].x + size.x;
		}
		return true;
	}

	public static void DrawCell(ColonistBarColonistDrawer drawer, Rect rect, Pawn colonist, Squad squad, Map pawnMap, bool highlight, bool reordering)
	{
		Rect rect2 = PanelRect(rect, colonist, squad);
		float num = Find.ColonistBar.GetEntryRectAlpha(rect2);
		if (pawnMap == null || pawnMap != Find.CurrentMap)
		{
			num = Mathf.Min(num, 0.4f);
		}
		if (reordering)
		{
			num *= 0.5f;
		}
		Color color = (GUI.color = new Color(1f, 1f, 1f, num));
		GUI.DrawTexture(rect2, ColonistBar.BGTex);
		float num2 = AverageMood(squad);
		if (num2 >= 0f)
		{
			Rect position = rect2.ContractedBy(2f);
			float num3 = position.height * num2;
			position.yMin = position.yMax - num3;
			position.height = num3;
			GUI.DrawTexture(position, (Texture2D)moodBGTexField.GetValue(null));
		}
		if (highlight)
		{
			int thickness = ((rect2.height <= 22f) ? 2 : 3);
			GUI.color = Color.white;
			Widgets.DrawBox(rect2, thickness);
			GUI.color = color;
		}
		if (AnySelected(squad))
		{
			drawSelectionOverlayMethod.Invoke(drawer, new object[] { colonist, rect2.ContractedBy(-2f * Find.ColonistBar.Scale) });
		}
		int num4 = DrawPortraits(rect2, squad, num);
		GUI.color = new Color(1f, 1f, 1f, num);
		if (num4 > 0)
		{
			DrawBadge(rect2, "+" + num4, num);
		}
		float num5 = 4f * Find.ColonistBar.Scale;
		float width = rect2.width + Find.ColonistBar.SpaceBetweenColonistsHorizontal - 2f;
		GenMapUI.DrawThingLabel(new Vector2(rect2.center.x, rect2.yMax - num5), squad.label.Truncate(width), new Color(1f, 1f, 1f, num));
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
	}

	private static int DrawPortraits(Rect panel, Squad squad, float alpha)
	{
		List<Pawn> list = Ordered(squad);
		if (list.Count == 0)
		{
			return 0;
		}
		int num = Mathf.Min(list.Count, 6);
		while (num > 1 && panel.width / (float)num < 17f)
		{
			num--;
		}
		float num2 = panel.width / (float)num;
		float num3 = ColonistBarColonistDrawer.PawnTextureSize.y / ColonistBarColonistDrawer.PawnTextureSize.x;
		float num4 = Mathf.Min(num2 * num3, panel.height * 1.5f);
		Vector2 size = new Vector2(Mathf.Round(num2), Mathf.Round(num4));
		GUI.color = new Color(1f, 1f, 1f, alpha);
		for (int i = 0; i < num; i++)
		{
			GUI.DrawTexture(new Rect(panel.x + (float)i * num2, panel.yMax - num4, num2, num4), PortraitsCache.Get(list[i], size, Rot4.South, ColonistBarColonistDrawer.PawnTextureCameraOffset, 1.28205f));
		}
		GUI.color = Color.white;
		return list.Count - num;
	}

	private static void DrawBadge(Rect panel, string text, float alpha)
	{
		Rect rect = new Rect(panel.xMax - 20f, panel.y + 1f, 19f, 14f);
		Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.55f * alpha));
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = new Color(1f, 1f, 1f, alpha);
		Widgets.Label(rect, text);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		Text.Font = GameFont.Small;
	}

	private static bool AnySelected(Squad squad)
	{
		List<object> selectedObjects = Find.Selector.SelectedObjects;
		List<Pawn> members = squad.Members;
		for (int i = 0; i < members.Count; i++)
		{
			if (selectedObjects.Contains(members[i]))
			{
				return true;
			}
		}
		return false;
	}

	public static void SelectSquad(Squad squad)
	{
		Selector selector = Find.Selector;
		selector.ClearSelection();
		List<Pawn> members = squad.Members;
		for (int i = 0; i < members.Count; i++)
		{
			if (members[i].Spawned)
			{
				selector.Select(members[i], playSound: false, forceDesignatorDeselect: false);
			}
		}
		if (members.Count != 0)
		{
			SoundDefOf.ThingSelected.PlayOneShotOnCamera();
			if (!AnyOnScreen(members))
			{
				CameraJumper.TryJump(members[0]);
			}
		}
	}

	private static bool AnyOnScreen(List<Pawn> members)
	{
		CellRect currentViewRect = Find.CameraDriver.CurrentViewRect;
		for (int i = 0; i < members.Count; i++)
		{
			if (members[i].Spawned && currentViewRect.Contains(members[i].Position))
			{
				return true;
			}
		}
		return false;
	}

	public static void OpenMemberMenu(Squad squad)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		list.Add(new FloatMenuOption("ASA_SquadBar_SelectAll".Translate(), () =>
		{
			SelectSquad(squad);
		}));
		list.Add(new FloatMenuOption("ASA_SquadBar_Expand".Translate(), () =>
		{
			squad.collapsed = false;
			Find.ColonistBar.MarkColonistsDirty();
		}));
		List<Pawn> list2 = Ordered(squad);
		for (int num = 0; num < list2.Count; num++)
		{
			Pawn member = list2[num];
			string text = member.LabelShortCap;
			if (member == squad.Leader)
			{
				text += " (" + "ASA_Squad_Leader".Translate() + ")";
			}
			list.Add(new FloatMenuOption(text, () =>
			{
				Find.Selector.ClearSelection();
				CameraJumper.TryJumpAndSelect(member);
			}, member, Color.white));
		}
		Find.WindowStack.Add(new FloatMenu(list));
	}

	public static string Tooltip(Squad squad)
	{
		List<Pawn> list = Ordered(squad);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < list.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append("\n");
			}
			stringBuilder.Append(list[i].LabelShortCap);
		}
		return squad.label + "\n\n" + stringBuilder?.ToString() + "\n\n" + "ASA_SquadBar_Tip".Translate();
	}
}
