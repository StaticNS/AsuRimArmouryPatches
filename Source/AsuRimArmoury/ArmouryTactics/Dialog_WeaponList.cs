using UnityEngine;
using Verse;

namespace ArmouryTactics;

public class Dialog_WeaponList : Window
{
	private readonly ModSettings_ArmouryTactics.Tab tab;

	private readonly string title;

	private Vector2 scroll;

	public override Vector2 InitialSize => new Vector2(720f, 700f);

	public Dialog_WeaponList(ModSettings_ArmouryTactics.Tab tab, string title)
	{
		this.tab = tab;
		this.title = title;
		doCloseX = true;
		doCloseButton = true;
		absorbInputAroundWindow = true;
		forcePause = false;
		OptionsDrawUtility.searchFilter = "";
	}

	public override void DoWindowContents(Rect inRect)
	{
		ModSettings_ArmouryTactics.selectedTab = tab;
		Text.Font = GameFont.Medium;
		Rect rect = new Rect(inRect.x, inRect.y, inRect.width, 34f);
		Widgets.Label(rect, title);
		Text.Font = GameFont.Small;
		Rect rect2 = new Rect(inRect.x, rect.yMax + 4f, inRect.width, 28f);
		OptionsDrawUtility.searchFilter = Widgets.TextField(rect2.ContractedBy(2f, 0f), OptionsDrawUtility.searchFilter);
		if (OptionsDrawUtility.searchFilter.NullOrEmpty())
		{
			GUI.color = new Color(1f, 1f, 1f, 0.4f);
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(rect2.x + 8f, rect2.y, rect2.width - 16f, rect2.height), "Search...");
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}
		Rect rect3 = new Rect(inRect.x, rect2.yMax + 4f, inRect.width, inRect.height - rect.height - rect2.height - 8f - Window.CloseButSize.y - 8f);
		Widgets.DrawMenuSection(rect3);
		Rect rect4 = rect3.ContractedBy(4f);
		Rect rect5 = new Rect(0f, 0f, rect4.width - 20f, (OptionsDrawUtility.lineNumber + 2) * 22);
		Widgets.BeginScrollView(rect4, ref scroll, rect5);
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.maxOneColumn = true;
		listing_Standard.Begin(rect5);
		listing_Standard.DrawList(rect4);
		listing_Standard.End();
		Widgets.EndScrollView();
	}

	public override void PreClose()
	{
		base.PreClose();
		Setup.CheckInvesions();
		Setup.SetupDW(processInversions: true);
	}
}
