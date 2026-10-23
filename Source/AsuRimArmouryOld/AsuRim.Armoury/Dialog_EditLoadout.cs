using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

public class Dialog_EditLoadout : Window
{
	private readonly WeaponPolicy policy;

	private readonly Pawn contextPawn;

	private Vector2 scrollPosition;

	private float lastHeight = 300f;

	private int activeTab;

	public override Vector2 InitialSize => new Vector2(620f, 560f);

	public Dialog_EditLoadout(WeaponPolicy policy, Pawn contextPawn = null)
	{
		this.policy = policy;
		this.contextPawn = contextPawn;
		doCloseX = true;
		doCloseButton = true;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Small;
		Rect rect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 40f);
		float y = rect.y;
		Rect rect2 = new Rect(rect.x, y, rect.width - 130f, 28f);
		policy.label = Widgets.TextField(rect2, policy.label);
		if (Widgets.ButtonText(new Rect(rect.xMax - 120f, y, 120f, 28f), "ASA_Edit_Delete".Translate()))
		{
			WeaponPolicyDatabase.Current?.Delete(policy);
			Close();
			return;
		}
		y += 36f;
		Widgets.DrawLineHorizontal(rect.x, y, rect.width);
		y += 8f;
		LoadoutEditorUI.DrawFields(new Rect(rect.x, y, rect.width, rect.yMax - y), policy, ref scrollPosition, ref lastHeight, ref activeTab, contextPawn);
	}

	public override void PostClose()
	{
		base.PostClose();
		LoadoutEditorUI.ReapplyToWearers(policy);
	}
}
