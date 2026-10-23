using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;

namespace AsuRim.Armoury;

internal static class GravshipSquadBoarding
{
	private static readonly ConditionalWeakTable<Dialog_BeginGravshipLaunch, HashSet<Squad>> ActiveDialogs = new ConditionalWeakTable<Dialog_BeginGravshipLaunch, HashSet<Squad>>();

	internal static HashSet<Squad> PendingSquads;

	internal static HashSet<Squad> SelectionFor(Dialog_BeginGravshipLaunch dialog)
	{
		return ActiveDialogs.GetOrCreateValue(dialog);
	}
}
