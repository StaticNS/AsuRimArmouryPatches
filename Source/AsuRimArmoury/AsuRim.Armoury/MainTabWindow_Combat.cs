using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

public class MainTabWindow_Combat : MainTabWindow
{
	private enum Section
	{
		Squads,
		Guards
	}

	private const float TabHeight = 32f;

	private Section section;

	private readonly MainTabWindow_Squads squads = new MainTabWindow_Squads();

	private readonly GuardRoster guards = new GuardRoster();

	private static readonly List<TabRecord> tabs = new List<TabRecord>();

	public override Vector2 RequestedTabSize => new Vector2(960f, 560f);

	public override void DoWindowContents(Rect inRect)
	{
		tabs.Clear();
		if (GuardStandDown.Active)
		{
			section = Section.Squads;
		}
		tabs.Add(new TabRecord("ASA_Tab_Squads".Translate(), () =>
		{
			section = Section.Squads;
		}, section == Section.Squads));
		if (!GuardStandDown.Active)
		{
			tabs.Add(new TabRecord("ASA_Tab_Guards".Translate(), () =>
			{
				section = Section.Guards;
				squads.Reset();
			}, section == Section.Guards));
		}
		Rect rect = new Rect(inRect);
		rect.yMin += 32f;
		Widgets.DrawMenuSection(rect);
		TabDrawer.DrawTabs(rect, tabs);
		tabs.Clear();
		Rect inRect2 = rect.ContractedBy(12f);
		if (section == Section.Squads)
		{
			squads.DoWindowContents(inRect2);
		}
		else
		{
			guards.Draw(inRect2);
		}
	}

	public override void PreClose()
	{
		base.PreClose();
		squads.Reset();
	}
}
