using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AsuRim.Armoury;

public class Dialog_AssignSquad : Window
{
	private const float RowHeight = 35f;

	private const float SeparatorHeight = 25f;

	private readonly Squad squad;

	private Vector2 scrollPosition;

	private string search = "";

	private static readonly List<Pawn> sorted = new List<Pawn>();

	public override Vector2 InitialSize => new Vector2(520f, 500f);

	public Dialog_AssignSquad(Squad squad)
	{
		this.squad = squad;
		doCloseX = true;
		doCloseButton = true;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Small;
		this.squad.CleanUp();
		List<Pawn> list = this.squad.Members;
		List<Pawn> list2 = Unassigned();
		List<Squad> list3 = OtherSquads();
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
			list = list.FindAll(Matches);
			list2 = list2.FindAll(Matches);
		}
		Rect outRect = new Rect(inRect);
		outRect.yMin += rect.height + 24f;
		outRect.yMax -= 40f;
		float num = (float)(list.Count + list2.Count) * 35f + 25f + 7f;
		for (int i = 0; i < list3.Count; i++)
		{
			num += 25f + (float)list3[i].Members.Count * 35f;
		}
		Rect viewRect = new Rect(0f, 0f, outRect.width, num);
		Widgets.AdjustRectsForScrollView(inRect, ref outRect, ref viewRect);
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		float y = 0f;
		Sort(list);
		for (int j = 0; j < sorted.Count; j++)
		{
			DrawRow(sorted[j], ref y, viewRect, j, assigned: true);
		}
		Widgets.ListSeparator(ref y, viewRect.width, "ASA_Squad_Unassigned".Translate());
		Sort(list2);
		for (int k = 0; k < sorted.Count; k++)
		{
			DrawRow(sorted[k], ref y, viewRect, k, assigned: false);
		}
		for (int l = 0; l < list3.Count; l++)
		{
			Squad squad = list3[l];
			Widgets.ListSeparator(ref y, viewRect.width, squad.label);
			List<Pawn> list4 = Eligible(squad.Members);
			if (!search.NullOrEmpty())
			{
				list4 = list4.FindAll(Matches);
			}
			Sort(list4);
			for (int m = 0; m < sorted.Count; m++)
			{
				DrawRow(sorted[m], ref y, viewRect, m, assigned: false);
			}
		}
		sorted.Clear();
		Widgets.EndScrollView();
	}

	private List<Pawn> Unassigned()
	{
		List<Pawn> list = new List<Pawn>();
		SquadManager current = SquadManager.Current;
		List<Pawn> list2 = Candidates();
		for (int i = 0; i < list2.Count; i++)
		{
			if (current?.SquadOf(list2[i]) == null)
			{
				list.Add(list2[i]);
			}
		}
		return list;
	}

	private List<Squad> OtherSquads()
	{
		List<Squad> list = new List<Squad>();
		List<Squad> list2 = SquadManager.Current?.Squads;
		if (list2 == null)
		{
			return list;
		}
		for (int i = 0; i < list2.Count; i++)
		{
			if (list2[i] != squad)
			{
				list2[i].CleanUp();
				if (Eligible(list2[i].Members).Count > 0)
				{
					list.Add(list2[i]);
				}
			}
		}
		return list;
	}

	private List<Pawn> Eligible(List<Pawn> pawns)
	{
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < pawns.Count; i++)
		{
			if (StanceRules.CanTakeOrders(pawns[i]) && !squad.Contains(pawns[i]))
			{
				list.Add(pawns[i]);
			}
		}
		return list;
	}

	private List<Pawn> Candidates()
	{
		List<Pawn> list = new List<Pawn>();
		foreach (Map map in Find.Maps)
		{
			foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
			{
				if (StanceRules.CanTakeOrders(item) && !squad.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	private bool Matches(Pawn pawn)
	{
		if (pawn.LabelCap.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string text = pawn.kindDef?.label;
		if (text != null)
		{
			return text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	private static void Sort(IEnumerable<Pawn> collection)
	{
		sorted.Clear();
		sorted.AddRange(collection);
		sorted.SortBy((Pawn p) => p.LabelShort);
	}

	private void DrawRow(Pawn pawn, ref float y, Rect viewRect, int index, bool assigned)
	{
		Rect rect = new Rect(0f, y, viewRect.width, 35f);
		y += 35f;
		if (index % 2 == 1)
		{
			Widgets.DrawLightHighlight(rect);
		}
		Rect rect2 = rect;
		rect2.width = rect.height;
		Widgets.ThingIcon(rect2, pawn);
		Rect rect3 = rect;
		rect3.xMin = rect.xMax - 175f;
		rect3 = rect3.ContractedBy(2f);
		if (assigned)
		{
			if (Widgets.ButtonText(rect3, "ASA_SquadRemove".Translate()))
			{
				this.squad.Remove(pawn);
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
		}
		else
		{
			Squad squad = SquadManager.Current?.SquadOf(pawn);
			string label = ((squad != null) ? ((string)"ASA_SquadMoveFrom".Translate(squad.label)) : ((string)"ASA_SquadAdd".Translate()));
			if (Widgets.ButtonText(rect3, label))
			{
				this.squad.Add(pawn);
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
		}
		Rect rect4 = rect;
		rect4.xMin = rect2.xMax + 10f;
		rect4.xMax = rect3.xMin - 10f;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect4, pawn.LabelCap);
		Text.Anchor = TextAnchor.UpperLeft;
	}
}
