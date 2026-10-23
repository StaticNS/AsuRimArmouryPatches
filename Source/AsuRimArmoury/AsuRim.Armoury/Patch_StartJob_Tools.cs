using HarmonyLib;
using Verse.AI;

namespace AsuRim.Armoury;

[HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
internal static class Patch_StartJob_Tools
{
	private static void Postfix(Pawn_JobTracker __instance, Job newJob)
	{
		ToolSwitching.OnJobStart(__instance.pawn, newJob);
	}
}
