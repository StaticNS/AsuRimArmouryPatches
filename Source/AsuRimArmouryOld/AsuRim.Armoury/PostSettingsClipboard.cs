using Verse;

namespace AsuRim.Armoury;

public static class PostSettingsClipboard
{
	private static bool copied;

	private static CombatStance stance;

	private static Area overwatchArea;

	private static bool markerHidden;

	public static bool HasCopied => copied;

	public static void Copy(CompGuardPost post)
	{
		if (post != null)
		{
			stance = post.stance;
			overwatchArea = post.OverwatchArea;
			markerHidden = (post.parent as Building_GuardMarker)?.hiddenIndividually ?? false;
			copied = true;
		}
	}

	public static void PasteInto(CompGuardPost post)
	{
		if (copied && post != null)
		{
			post.stance = stance;
			if (overwatchArea != null && post.parent?.Map != null && overwatchArea.Map == post.parent.Map)
			{
				post.OverwatchArea = overwatchArea;
			}
			if (post.parent is Building_GuardMarker building_GuardMarker)
			{
				building_GuardMarker.hiddenIndividually = markerHidden;
			}
		}
	}
}
