using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

public class Building_GuardMarker : Building
{
	private CompGuardPost post;

	public bool hiddenIndividually;

	public CompGuardPost Post => post ?? (post = this.TryGetComp<CompGuardPost>());

	public bool ShouldDraw
	{
		get
		{
			if (!GuardStandDown.Active)
			{
				if (!hiddenIndividually)
				{
					return GuardMarkerVisibility.ShowMarkers;
				}
				return false;
			}
			return true;
		}
	}

	public override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		if (ShouldDraw)
		{
			base.DrawAt(drawLoc, flip);
		}
	}

	public override void DrawGUIOverlay()
	{
		if (ShouldDraw)
		{
			base.DrawGUIOverlay();
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (GuardStandDown.Active)
		{
			yield break;
		}
		yield return new Command_Toggle
		{
			defaultLabel = "ASA_Post_HideThis".Translate(),
			defaultDesc = "ASA_Post_HideThis_Desc".Translate(),
			icon = StanceIcons.Get("UI/Stances/ASA_EyeClosed", TexCommand.ForbidOff),
			isActive = () => hiddenIndividually,
			toggleAction = () =>
			{
				bool flag = !hiddenIndividually;
				foreach (object selectedObject in Find.Selector.SelectedObjects)
				{
					if (selectedObject is Building_GuardMarker building_GuardMarker)
					{
						building_GuardMarker.hiddenIndividually = flag;
					}
				}
			}
		};
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref hiddenIndividually, "hiddenIndividually", defaultValue: false);
	}
}
