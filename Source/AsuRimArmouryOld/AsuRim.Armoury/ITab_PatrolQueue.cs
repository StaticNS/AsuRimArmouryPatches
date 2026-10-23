using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AsuRim.Armoury;

public class ITab_PatrolQueue : ITab
{
	private const float CellSize = 26f;

	private const float Padding = 2f;

	private const int MaxVisibleCells = 12;

	private static readonly List<CompGuardPost> routeBuffer = new List<CompGuardPost>();

	private Vector2 scroll;

	private CompGuardPost Post => SelThing?.TryGetComp<CompGuardPost>();

	internal Rect PatrolTabRect => TabRect;

	internal bool PatrolTabStillValid => StillValid;

	internal void FillPatrolTab()
	{
		FillTab();
	}

	internal void DrawPatrolExtraOnGUI()
	{
		ExtraOnGUI();
	}

	public override bool IsVisible
	{
		get
		{
			if (GuardStandDown.Active)
			{
				return false;
			}
			CompGuardPost post = Post;
			if (post != null && post.Kind == PostKind.Patrol)
			{
				return RouteCount(post) > 1;
			}
			return false;
		}
	}

	public ITab_PatrolQueue()
	{
		labelKey = "ASA_Post_QueueGizmo";
	}

	private static int RouteCount(CompGuardPost post)
	{
		GuardRegistry guardRegistry = GuardRegistry.For(post.parent.Map);
		if (guardRegistry == null)
		{
			return 0;
		}
		guardRegistry.Route(post.group, routeBuffer);
		return routeBuffer.Count;
	}

	protected override void UpdateSize()
	{
		CompGuardPost post = Post;
		int num = Mathf.Min((post != null) ? RouteCount(post) : 0, 12);
		size = new Vector2(Mathf.Max((float)num * 26f, 90f), 30f);
	}

	protected override void FillTab()
	{
		CompGuardPost post = Post;
		if (post?.parent?.Map == null)
		{
			return;
		}
		GuardRegistry guardRegistry = GuardRegistry.For(post.parent.Map);
		if (guardRegistry == null)
		{
			return;
		}
		guardRegistry.Route(post.group, routeBuffer);
		int count = routeBuffer.Count;
		Rect rect = new Rect(0f, 2f, size.x, 26f);
		Rect rect2 = new Rect(0f, 0f, (float)count * 26f, 26f);
		bool flag = rect2.width > rect.width;
		if (flag)
		{
			Widgets.BeginScrollView(rect, ref scroll, rect2);
		}
		Rect rect3 = (flag ? rect2 : rect);
		float num = rect3.x;
		for (int i = 0; i < count; i++)
		{
			CompGuardPost compGuardPost = routeBuffer[i];
			Rect rect4 = new Rect(num, rect3.y, 26f, 26f);
			Rect rect5 = rect4.ContractedBy(1f);
			bool flag2 = compGuardPost == post;
			Widgets.DrawBoxSolid(rect5, flag2 ? new Color(0.55f, 0.14f, 0.16f) : new Color(0.24f, 0.24f, 0.26f));
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect5, (i + 1).ToString());
			Text.Anchor = TextAnchor.UpperLeft;
			TooltipHandler.TipRegion(rect4, "ASA_Post_QueuePosition_Desc".Translate(i + 1));
			if (!flag2 && Widgets.ButtonInvisible(rect4))
			{
				guardRegistry.Reorder(post, i);
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			num += 26f;
		}
		if (flag)
		{
			Widgets.EndScrollView();
		}
	}
}
