using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class WorkGiver_ManPost : WorkGiver_Scanner
{
	private static readonly List<CompGuardPost> routeBuffer = new List<CompGuardPost>();

	public override PathEndMode PathEndMode => PathEndMode.OnCell;

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		GuardRegistry guardRegistry = GuardRegistry.For(pawn.Map);
		if (guardRegistry == null)
		{
			yield break;
		}
		List<CompGuardPost> posts = guardRegistry.Posts;
		for (int i = 0; i < posts.Count; i++)
		{
			CompGuardPost compGuardPost = posts[i];
			if (compGuardPost?.parent != null && compGuardPost.parent.Spawned && compGuardPost.IsAssigned(pawn))
			{
				yield return compGuardPost.parent;
			}
		}
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		if (GuardStandDown.Active)
		{
			return true;
		}
		GuardRegistry guardRegistry = GuardRegistry.For(pawn.Map);
		if (guardRegistry == null)
		{
			return true;
		}
		List<CompGuardPost> posts = guardRegistry.Posts;
		for (int i = 0; i < posts.Count; i++)
		{
			if (posts[i] != null && posts[i].IsAssigned(pawn))
			{
				return false;
			}
		}
		return true;
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		CompGuardPost compGuardPost = t.TryGetComp<CompGuardPost>();
		if (compGuardPost == null || !compGuardPost.IsAssigned(pawn))
		{
			return false;
		}
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return false;
		}
		if (compGuardPost.Kind == PostKind.Mech && !pawn.RaceProps.IsMechanoid)
		{
			return false;
		}
		IntVec3 standCell = compGuardPost.StandCell;
		if (!standCell.IsValid || !standCell.Standable(pawn.Map))
		{
			return false;
		}
		if (!pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Deadly))
		{
			return false;
		}
		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!HasJobOnThing(pawn, t, forced))
		{
			return null;
		}
		CompGuardPost compGuardPost = t.TryGetComp<CompGuardPost>();
		if (compGuardPost.Kind != PostKind.Patrol)
		{
			return JobMaker.MakeJob(ASA_PostJobDefOf.ASA_ManPost, t, compGuardPost.StandCell);
		}
		CompGuardPost compGuardPost2 = NextNode(pawn, compGuardPost);
		if (compGuardPost2?.parent == null)
		{
			return null;
		}
		return JobMaker.MakeJob(ASA_PostJobDefOf.ASA_Patrol, compGuardPost2.parent, compGuardPost2.StandCell);
	}

	private static CompGuardPost NextNode(Pawn pawn, CompGuardPost anyNode)
	{
		GuardRegistry guardRegistry = GuardRegistry.For(pawn.Map);
		if (guardRegistry == null)
		{
			return null;
		}
		guardRegistry.Route(anyNode.group, routeBuffer);
		if (routeBuffer.Count == 0)
		{
			return null;
		}
		int num = CompArsenal.For(pawn)?.patrolIndex ?? 0;
		if (num < 0 || num >= routeBuffer.Count)
		{
			num = 0;
		}
		return routeBuffer[num];
	}
}
