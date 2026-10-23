using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

public static class LoadoutPanelToggleGizmo
{
	public static IEnumerable<Gizmo> For(Pawn pawn)
	{
		yield return new Command_Action
		{
			defaultLabel = ArmouryText.Get("ASA_Panel_Expand"),
			defaultDesc = ArmouryText.Get("ASA_Panel_ExpandDesc"),
			icon = TexButton.Plus,
			action = () =>
			{
				ArmourySettings.loadoutPanel = true;
			}
		};
	}
}
