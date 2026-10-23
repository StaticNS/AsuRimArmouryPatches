using UnityEngine;
using Verse;

namespace AsuRim.Armoury;

internal static class WeaponIconSuppressor
{
	private static Rect expected;

	private static bool armed;

	internal static void SuppressNextFor(Rect cell)
	{
		expected = new Rect(cell.x, cell.y + cell.height * 1.05f, cell.width, cell.height).ScaledBy(0.75f);
		armed = true;
	}

	internal static bool ShouldSkip(Rect rect)
	{
		if (!armed)
		{
			return false;
		}
		if (Mathf.Abs(rect.x - expected.x) > 0.5f || Mathf.Abs(rect.y - expected.y) > 0.5f)
		{
			return false;
		}
		if (Mathf.Abs(rect.width - expected.width) > 0.5f || Mathf.Abs(rect.height - expected.height) > 0.5f)
		{
			return false;
		}
		armed = false;
		return true;
	}
}
