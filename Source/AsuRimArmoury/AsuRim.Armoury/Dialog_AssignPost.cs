using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AsuRim.Armoury;

public class Dialog_AssignPost : Window
{
	private enum DragAction
	{
		None,
		Assign,
		Unassign
	}

	private const float RowHeight = 35f;

	private readonly CompGuardPost post;

	private Vector2 scrollPosition;

	private string search = "";

	private static readonly List<Pawn> sorted = new List<Pawn>();

	private static DragAction dragAction = DragAction.None;

	private static readonly HashSet<Pawn> dragApplied = new HashSet<Pawn>();

	public override Vector2 InitialSize => new Vector2(520f, 560f);

	public Dialog_AssignPost(CompGuardPost post)
	{
		this.post = post;
		doCloseX = true;
		doCloseButton = true;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Small;
		if (!Input.GetMouseButton(0))
		{
			dragAction = DragAction.None;
			dragApplied.Clear();
		}
		List<Squad> list = ((post.Kind == PostKind.Mech) ? new List<Squad>() : (SquadManager.Current?.Squads ?? new List<Squad>()));
		List<Pawn> list2 = ((post.Squad != null) ? new List<Pawn>() : post.Manning);
		List<Pawn> list3 = Candidates();
		if (!search.NullOrEmpty())
		{
			list2 = list2.FindAll(Matches);
			list3 = list3.FindAll(Matches);
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
		Rect outRect = new Rect(inRect);
		outRect.yMin += rect.height + 24f;
		outRect.yMax -= 40f;
		float height = (float)(list.Count + list2.Count + list3.Count) * 35f + 24f;
		Rect viewRect = new Rect(0f, 0f, outRect.width, height);
		Widgets.AdjustRectsForScrollView(inRect, ref outRect, ref viewRect);
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		float y = 0f;
		for (int i = 0; i < list.Count; i++)
		{
			DrawSquadRow(list[i], ref y, viewRect, i);
		}
		if (list.Count > 0)
		{
			using (new TextBlock(Widgets.SeparatorLineColor))
			{
				Widgets.DrawLineHorizontal(0f, y + 3.5f, viewRect.width);
			}
			y += 7f;
		}
		Sort(list2);
		for (int j = 0; j < sorted.Count; j++)
		{
			DrawPawnRow(sorted[j], ref y, viewRect, j, isAssigned: true);
		}
		if (list2.Count > 0)
		{
			using (new TextBlock(Widgets.SeparatorLineColor))
			{
				Widgets.DrawLineHorizontal(0f, y + 3.5f, viewRect.width);
			}
			y += 7f;
		}
		Sort(list3);
		for (int k = 0; k < sorted.Count; k++)
		{
			DrawPawnRow(sorted[k], ref y, viewRect, k, isAssigned: false);
		}
		sorted.Clear();
		Widgets.EndScrollView();
	}

	private void DrawSquadRow(Squad squad, ref float y, Rect viewRect, int index)
	{
		Rect rect = new Rect(0f, y, viewRect.width, 35f);
		y += 35f;
		if (index % 2 == 1)
		{
			Widgets.DrawLightHighlight(rect);
		}
		Rect rect2 = rect;
		rect2.xMin = rect.xMax - 175f;
		rect2 = rect2.ContractedBy(2f);
		bool flag = post.Squad == squad;
		if (Widgets.ButtonText(rect2, flag ? "ASA_SquadRemove".Translate() : "ASA_Post_AssignSquad".Translate()))
		{
			Squad want = (flag ? null : squad);
			ApplyAcrossRoute((CompGuardPost p) =>
			{
				p.Squad = want;
			});
			SoundDefOf.Click.PlayOneShotOnCamera();
		}
		Rect rect3 = rect;
		rect3.xMin = 10f;
		rect3.xMax = rect2.xMin - 10f;
		Text.Anchor = TextAnchor.MiddleLeft;
		squad.CleanUp();
		Widgets.Label(rect3, squad.label + " (" + squad.Members.Count + ")");
		Text.Anchor = TextAnchor.UpperLeft;
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

	private List<Pawn> Candidates()
	{
		List<Pawn> list = new List<Pawn>();
		if (post.Squad != null)
		{
			return list;
		}
		Map map = post.parent.Map;
		if (map == null)
		{
			return list;
		}
		foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
		{
			if (StanceRules.CanTakeOrders(item) && (post.Kind != PostKind.Mech || item.RaceProps.IsMechanoid) && !post.IsAssigned(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	private void ApplyAcrossRoute(Action<CompGuardPost> apply)
	{
		if (post.Kind == PostKind.Patrol)
		{
			ApplyAcrossGroup(apply);
		}
		else
		{
			apply(post);
		}
	}

	private void ApplyAcrossGroup(Action<CompGuardPost> apply)
	{
		GuardRegistry guardRegistry = GuardRegistry.For(post.parent.Map);
		if (guardRegistry == null)
		{
			apply(post);
			return;
		}
		List<CompGuardPost> posts = guardRegistry.Posts;
		bool flag = false;
		for (int i = 0; i < posts.Count; i++)
		{
			CompGuardPost compGuardPost = posts[i];
			if (compGuardPost?.parent != null && compGuardPost.parent.Spawned && compGuardPost.Kind == post.Kind && compGuardPost.group == post.group)
			{
				apply(compGuardPost);
				flag = true;
			}
		}
		if (!flag)
		{
			apply(post);
		}
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

	private void TryAssign(Pawn pawn)
	{
		CompGuardPost conflict = GuardRegistry.For(post.parent.Map)?.FindConflict(pawn, post);
		if (conflict == null)
		{
			ApplyAcrossRoute((CompGuardPost p) =>
			{
				p.Assign(pawn);
			});
			SoundDefOf.Click.PlayOneShotOnCamera();
			return;
		}
		string text;
		if (conflict.Kind == PostKind.Patrol)
		{
			text = "ASA_Guards_Route".Translate(conflict.group.ToString());
		}
		else
		{
			text = ((conflict.Kind == PostKind.Mech) ? ((string)"ASA_Guards_MechPost".Translate()) : ((string)"ASA_Guards_Post".Translate(conflict.group.ToString())));
		}
		Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ASA_Post_Reassign_Confirm".Translate(pawn.LabelShort, text), () =>
		{
			GuardRegistry.UnassignAcrossGroup(conflict, pawn);
			ApplyAcrossRoute((CompGuardPost p) =>
			{
				p.Assign(pawn);
			});
			SoundDefOf.Click.PlayOneShotOnCamera();
		}));
	}

	private void DragAssign(Pawn pawn)
	{
		CompGuardPost compGuardPost = GuardRegistry.For(post.parent.Map)?.FindConflict(pawn, post);
		if (compGuardPost != null)
		{
			GuardRegistry.UnassignAcrossGroup(compGuardPost, pawn);
		}
		ApplyAcrossRoute((CompGuardPost p) =>
		{
			p.Assign(pawn);
		});
		SoundDefOf.Designate_DragStandard_Changed_NoCam.PlayOneShotOnCamera();
	}

	private static void Sort(IEnumerable<Pawn> collection)
	{
		sorted.Clear();
		sorted.AddRange(collection);
		sorted.Sort((Pawn a, Pawn b) =>
		{
			int num = Category(a);
			int num2 = Category(b);
			return (num == num2) ? string.Compare(a.LabelShort, b.LabelShort, StringComparison.OrdinalIgnoreCase) : num.CompareTo(num2);
		});
	}

	private void DrawPawnRow(Pawn pawn, ref float y, Rect viewRect, int index, bool isAssigned)
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
		if (isAssigned)
		{
			if (Widgets.ButtonText(rect3, "ASA_SquadRemove".Translate()))
			{
				ApplyAcrossRoute((CompGuardPost p) =>
				{
					p.Unassign(pawn);
				});
				SoundDefOf.Click.PlayOneShotOnCamera();
				dragAction = DragAction.Unassign;
				dragApplied.Add(pawn);
			}
			else if (dragAction == DragAction.Unassign && Input.GetMouseButton(0) && Mouse.IsOver(rect3) && dragApplied.Add(pawn))
			{
				ApplyAcrossRoute((CompGuardPost p) =>
				{
					p.Unassign(pawn);
				});
				SoundDefOf.Designate_DragStandard_Changed_NoCam.PlayOneShotOnCamera();
			}
		}
		else if (Widgets.ButtonText(rect3, "ASA_SquadAdd".Translate()))
		{
			TryAssign(pawn);
			dragAction = DragAction.Assign;
			dragApplied.Add(pawn);
		}
		else if (dragAction == DragAction.Assign && Input.GetMouseButton(0) && Mouse.IsOver(rect3) && dragApplied.Add(pawn))
		{
			DragAssign(pawn);
		}
		Rect rect4 = rect;
		rect4.xMin = rect2.xMax + 10f;
		rect4.xMax = rect3.xMin - 10f;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect4, pawn.LabelCap);
		Text.Anchor = TextAnchor.UpperLeft;
	}
}
