using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AsuRim.Armoury;

public class MainTabWindow_Squads : MainTabWindow
{
	private const float RowHeight = 90f;

	private const float HeaderHeight = 34f;

	private const float PortraitSize = 52f;

	private const float ControlWidth = 240f;

	private Vector2 scrollPosition;

	private float lastContentHeight = 400f;

	private string search = "";

	private static readonly List<Squad> filtered = new List<Squad>();

	private Squad expandedSquad;

	private const float PortraitGap = 2f;

	private const float PortraitsTop = 6f;

	private static readonly Color LeaderColor = new Color(0.9f, 0.78f, 0.35f);

	public override Vector2 RequestedTabSize => new Vector2(900f, 520f);

	public void Reset()
	{
		expandedSquad = null;
	}

	public override void DoWindowContents(Rect inRect)
	{
		SquadManager current = SquadManager.Current;
		if (current != null)
		{
			Rect rect = new Rect(inRect.x, inRect.y, inRect.width, 34f);
			if (Widgets.ButtonText(new Rect(rect.x, rect.y, 160f, 30f), "ASA_NewSquad".Translate()))
			{
				current.NewSquad();
			}
			Rect rect2 = new Rect(rect.xMax + 10f, rect.y, 220f, 30f);
			search = Widgets.TextField(rect2, search);
			if (search.NullOrEmpty())
			{
				Text.Anchor = TextAnchor.MiddleLeft;
				GUI.color = new Color(1f, 1f, 1f, 0.4f);
				Widgets.Label(new Rect(rect2.x + 6f, rect2.y, rect2.width - 12f, rect2.height), "ASA_Post_Search".Translate());
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			Rect outRect = new Rect(inRect.x, rect.yMax + 6f, inRect.width, inRect.height - rect.height - 6f);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, lastContentHeight);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			float num = 0f;
			List<Squad> list = Filtered(current.Squads);
			for (int i = 0; i < list.Count; i++)
			{
				Squad squad = list[i];
				float num2 = RowHeightFor(viewRect.width, squad);
				DrawSquadRow(new Rect(0f, num, viewRect.width, num2), squad, current);
				num += num2;
				Widgets.DrawLineHorizontal(0f, num, viewRect.width);
				num += 4f;
			}
			if (list.Count == 0)
			{
				Widgets.NoneLabelCenteredVertically(new Rect(0f, 0f, viewRect.width, 60f), "ASA_NoSquads".Translate());
				num = 60f;
			}
			lastContentHeight = Mathf.Max(num, 1f);
			Widgets.EndScrollView();
		}
	}

	private List<Squad> Filtered(List<Squad> squads)
	{
		filtered.Clear();
		if (search.NullOrEmpty())
		{
			filtered.AddRange(squads);
			return filtered;
		}
		for (int i = 0; i < squads.Count; i++)
		{
			Squad squad = squads[i];
			if (squad.label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || MatchesMember(squad))
			{
				filtered.Add(squad);
			}
		}
		return filtered;
	}

	private bool MatchesMember(Squad squad)
	{
		List<Pawn> members = squad.Members;
		for (int i = 0; i < members.Count; i++)
		{
			Pawn pawn = members[i];
			if (pawn.LabelCap.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			string text = pawn.kindDef?.label;
			if (text != null && text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private float RowHeightFor(float rowWidth, Squad squad)
	{
		if (squad != expandedSquad)
		{
			return 90f;
		}
		int num = PortraitsPerRow(rowWidth);
		int count = squad.Members.Count;
		int num2 = ((num <= 0) ? 1 : Mathf.Max(1, Mathf.CeilToInt((float)count / (float)num)));
		return 6f + (float)num2 * 54f + 8f;
	}

	private static int PortraitsPerRow(float rowWidth)
	{
		float num = PortraitsLeft();
		float num2 = rowWidth - PortraitsRightMargin() - num;
		return Mathf.Max(1, Mathf.FloorToInt(num2 / 54f));
	}

	private static float PortraitsLeft()
	{
		return 6f + Mathf.Max(190f, 150f) + 12f;
	}

	private static float PortraitsRightMargin()
	{
		return 222f;
	}

	private static void OpenMemberMenu(Squad squad, Pawn member)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		if (squad.Leader == member)
		{
			list.Add(new FloatMenuOption("ASA_Squad_ClearLeader".Translate(), () =>
			{
				squad.SetLeader(null);
			}));
		}
		else
		{
			list.Add(new FloatMenuOption("ASA_Squad_MakeLeader".Translate(), () =>
			{
				squad.SetLeader(member);
			}));
		}
		list.Add(new FloatMenuOption("ASA_Squad_Select".Translate(), () =>
		{
			CameraJumper.TryJumpAndSelect(member);
		}));
		list.Add(new FloatMenuOption("ASA_SquadRemove".Translate(), () =>
		{
			squad.Remove(member);
		}));
		Find.WindowStack.Add(new FloatMenu(list));
	}

	private void DrawSquadRow(Rect rect, Squad squad, SquadManager manager)
	{
		Widgets.DrawHighlightIfMouseover(rect);
		squad.CleanUp();
		float num = rect.y + 5f;
		float num2 = rect.y + 33f;
		Rect rect2 = new Rect(rect.x + 6f, num, 164f, 26f);
		Widgets.DrawHighlightIfMouseover(rect2);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect2, squad.label.Truncate(rect2.width - 4f));
		Text.Anchor = TextAnchor.UpperLeft;
		if (Widgets.ButtonInvisible(rect2))
		{
			Find.Selector.ClearSelection();
			List<Pawn> members = squad.Members;
			for (int i = 0; i < members.Count; i++)
			{
				Pawn pawn = members[i];
				if (pawn.Spawned && pawn.Map == Find.CurrentMap)
				{
					Find.Selector.Select(pawn, playSound: false);
				}
			}
			SoundDefOf.Click.PlayOneShotOnCamera();
			Find.WindowStack.TryRemove(typeof(MainTabWindow_Combat), doCloseSound: false);
			return;
		}
		TooltipHandler.TipRegion(rect2, "ASA_SquadBar_SelectAll".Translate());
		Rect rect3 = new Rect(rect2.xMax + 2f, num + 1f, 24f, 24f);
		if (Widgets.ButtonImage(rect3, TexUI.RenameTex))
		{
			Find.WindowStack.Add(new Dialog_RenameSquad(squad));
		}
		TooltipHandler.TipRegion(rect3, "Rename".Translate());
		if (Widgets.ButtonText(new Rect(rect.x + 6f, num2, 150f, 26f), squad.Stance.LabelKey().Translate()))
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			foreach (CombatStance value in Enum.GetValues(typeof(CombatStance)))
			{
				if (!value.Offerable())
				{
					continue;
				}
				CombatStance choice = value;
				Action<Rect> mouseoverGuiAction = (Rect rect9) =>
				{
					TooltipHandler.TipRegion(rect9, choice.LabelKey().Translate() + "\n\n" + choice.DescKey().Translate());
				};
				if (choice.SquadOnly() && squad.Leader == null)
				{
					list.Add(new FloatMenuOption(choice.LabelKey().Translate() + " (" + "ASA_Squad_NoLeader".Translate() + ")", null, StanceIcons.Get(choice), Color.white, MenuOptionPriority.Default, mouseoverGuiAction));
					continue;
				}
				list.Add(new FloatMenuOption(choice.LabelKey().Translate(), () =>
				{
					squad.Stance = choice;
				}, StanceIcons.Get(choice), Color.white, MenuOptionPriority.Default, mouseoverGuiAction));
			}
			Find.WindowStack.Add(new FloatMenu(list));
		}
		Rect rect4 = new Rect(rect.xMax - 92f - 6f, num, 92f, 26f);
		if (Widgets.ButtonText(rect4, "ASA_SquadManage".Translate()))
		{
			Find.WindowStack.Add(new Dialog_AssignSquad(squad));
		}
		if (Widgets.ButtonText(new Rect(rect.xMax - 92f - 6f, num2, 92f, 26f), "ASA_DeleteSquad".Translate()))
		{
			manager.Delete(squad);
			return;
		}
		DrawSquadAreaButton(new Rect(rect.xMax - 92f - 6f, num2 + 28f, 92f, 26f), squad);
		Rect rect5 = new Rect(rect4.xMin - 118f, num, 112f, 26f);
		bool collapsed = squad.collapsed;
		Widgets.CheckboxLabeled(rect5, "ASA_Squad_Collapse".Translate(), ref squad.collapsed, disabled: false, null, null, placeCheckboxNearText: true);
		TooltipHandler.TipRegion(rect5, "ASA_Squad_Collapse_Desc".Translate());
		if (collapsed != squad.collapsed)
		{
			Find.ColonistBar.MarkColonistsDirty();
		}
		float num3 = rect.x + PortraitsLeft();
		float num4 = rect.x + rect.width - PortraitsRightMargin();
		bool flag = squad == expandedSquad;
		int num5 = PortraitsPerRow(rect.width);
		float num6 = num3;
		float num7 = rect.y + 6f;
		bool flag2 = false;
		List<Pawn> members2 = squad.Members;
		for (int num8 = 0; num8 < members2.Count; num8++)
		{
			bool num9;
			if (!flag)
			{
				num9 = num6 + 52f > num4;
			}
			else
			{
				if (num8 <= 0)
				{
					goto IL_0636;
				}
				num9 = num8 % num5 == 0;
			}
			if (num9)
			{
				if (!flag)
				{
					Rect rect6 = new Rect(num6, rect.y + 20f, 60f, 24f);
					if (Widgets.ButtonText(rect6, "+" + (members2.Count - num8), drawBackground: false))
					{
						expandedSquad = squad;
						SoundDefOf.Click.PlayOneShotOnCamera();
					}
					TooltipHandler.TipRegion(rect6, "ASA_SquadBar_Expand".Translate());
					break;
				}
				if (!flag2)
				{
					flag2 = true;
					Rect rect7 = new Rect(num6, rect.y + 20f, 24f, 24f);
					if (Widgets.ButtonText(rect7, "-", drawBackground: false))
					{
						expandedSquad = null;
						SoundDefOf.Click.PlayOneShotOnCamera();
					}
					TooltipHandler.TipRegion(rect7, "ASA_Squad_ShowLess".Translate());
				}
				num6 = num3;
				num7 += 54f;
			}
			goto IL_0636;
			IL_0636:
			Pawn pawn2 = members2[num8];
			Rect rect8 = new Rect(num6, num7, 52f, 52f);
			GUI.DrawTexture(rect8, PortraitsCache.Get(pawn2, new Vector2(52f, 52f), Rot4.South));
			Widgets.DrawHighlightIfMouseover(rect8);
			bool flag3 = pawn2 == squad.Leader;
			if (flag3)
			{
				GUI.color = LeaderColor;
				Widgets.DrawBox(rect8, 2);
				GUI.color = Color.white;
			}
			string text = pawn2.LabelShortCap;
			if (flag3)
			{
				text += "\n" + "ASA_Squad_Leader".Translate();
			}
			TooltipHandler.TipRegion(rect8, text + "\n\n" + "ASA_SquadPortraitHint".Translate());
			if (Mouse.IsOver(rect8) && Event.current.type == EventType.mouseDown && Event.current.button == 1)
			{
				Event.current.Use();
				squad.Remove(pawn2);
				return;
			}
			if (Widgets.ButtonInvisible(rect8))
			{
				OpenMemberMenu(squad, pawn2);
				return;
			}
			num6 += 54f;
		}
		if (members2.Count == 0)
		{
			Text.Anchor = TextAnchor.MiddleLeft;
			GUI.color = new Color(1f, 1f, 1f, 0.5f);
			Widgets.Label(new Rect(num3, rect.y, 240f, rect.height), "ASA_SquadEmpty".Translate());
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
	}

	private static void DrawSquadAreaButton(Rect rect, Squad squad)
	{
		Map currentMap = Find.CurrentMap;
		List<Pawn> members = squad.Members;
		int num = 0;
		for (int i = 0; i < members.Count; i++)
		{
			if (members[i].Spawned && members[i].Map == currentMap)
			{
				num++;
			}
		}
		bool flag = currentMap == null || (members.Count > 0 && num * 2 < members.Count);
		string str = ((squad.Area != null) ? squad.Area.Label : ((string)"ASA_Squad_NoZone".Translate()));
		bool flag2 = Widgets.ButtonText(rect, str.Truncate(rect.width - 8f), drawBackground: true, doMouseoverSound: true, !flag);
		if (flag)
		{
			TooltipHandler.TipRegion(rect, "ASA_Squad_AreaOffMap".Translate());
			return;
		}
		TooltipHandler.TipRegion(rect, "ASA_Squad_ZoneHint".Translate());
		if (!flag2)
		{
			return;
		}
		List<FloatMenuOption> list = new List<FloatMenuOption>
		{
			new FloatMenuOption("ASA_Squad_NoZone".Translate(), () =>
			{
				squad.Area = null;
			})
		};
		List<Area> allAreas = currentMap.areaManager.AllAreas;
		for (int num2 = 0; num2 < allAreas.Count; num2++)
		{
			if (allAreas[num2].AssignableAsAllowed())
			{
				Area captured = allAreas[num2];
				list.Add(new FloatMenuOption(captured.Label, () =>
				{
					squad.Area = captured;
				}));
			}
		}
		Find.WindowStack.Add(new FloatMenu(list));
	}
}
