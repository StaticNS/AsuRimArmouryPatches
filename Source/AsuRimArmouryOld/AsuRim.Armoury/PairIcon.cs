using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

public static class PairIcon
{
	private const float InsetFraction = 0.18f;

	public static void Rects(Rect bounds, out Rect back, out Rect front)
	{
		float num = bounds.width * 0.18f;
		back = new Rect(bounds.x + num, bounds.y, bounds.width - num, bounds.height - num);
		front = new Rect(bounds.x, bounds.y + num, bounds.width - num, bounds.height - num);
	}

	public static void Draw(Rect bounds, ThingWithComps mainThing, ThingWithComps offThing, float scale, bool lowLight)
	{
		Rects(bounds, out var back, out var front);
		if (offThing != null)
		{
			Widgets.ThingIcon(back, offThing, 1f, null, stackOfOne: false, scale, lowLight);
		}
		if (mainThing != null)
		{
			Widgets.ThingIcon(front, mainThing, 1f, null, stackOfOne: false, scale, lowLight);
		}
	}
}
