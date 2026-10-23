using HarmonyLib;
using Verse;
using Verse.AI;
using System.Reflection;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
internal static class Patch_StartJob_Tools
{
	private static readonly FieldInfo pawnField = AccessTools.Field(typeof(Pawn_JobTracker), "pawn");

	private static void Postfix(Pawn_JobTracker __instance, Job newJob)
	{
		ToolSwitching.OnJobStart(pawnField?.GetValue(__instance) as Pawn, newJob);
	}
}
