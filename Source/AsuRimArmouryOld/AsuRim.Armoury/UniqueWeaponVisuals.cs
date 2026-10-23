using RimWorld;
using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

internal static class UniqueWeaponVisuals
{
	private static readonly Color GoldGlow = new Color(0.95f, 0.78f, 0.15f);

	internal static bool IsUnique(Thing thing)
	{
		if (thing is ThingWithComps thingWithComps)
		{
			return thingWithComps.GetComp<CompUniqueWeapon>() != null;
		}
		return false;
	}

	internal static void DrawGlow(Rect rect, Thing thing)
	{
		if (IsUnique(thing))
		{
			Texture2D iconFor = Widgets.GetIconFor(thing.def, (thing as ThingWithComps)?.Stuff);
			if (!(iconFor == null))
			{
				float margin = Mathf.Min(3f, rect.width * 0.08f);
				Rect position = rect.ExpandedBy(margin);
				Color color = GUI.color;
				GUI.color = GoldGlow;
				GUI.DrawTexture(position, iconFor);
				GUI.color = color;
			}
		}
	}
}
