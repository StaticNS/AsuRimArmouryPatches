using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

public class GuardRegistry(Map map) : MapComponent(map)
{
	private readonly List<CompGuardPost> posts = new List<CompGuardPost>();

	private bool conversionPending;

	private static readonly List<CompGuardPost> reorderBuffer = new List<CompGuardPost>();

	public List<CompGuardPost> Posts => posts;

	public static GuardRegistry For(Map map)
	{
		return map?.GetComponent<GuardRegistry>();
	}

	public void Register(CompGuardPost post)
	{
		if (post != null && !posts.Contains(post))
		{
			posts.Add(post);
		}
		if (GuardStandDown.Active)
		{
			conversionPending = true;
		}
	}

	public override void FinalizeInit()
	{
		base.FinalizeInit();
		if (GuardStandDown.Active)
		{
			conversionPending = true;
		}
	}

	public override void MapComponentUpdate()
	{
		if (conversionPending)
		{
			conversionPending = false;
			SurveillanceShiftConversion.Run(map);
		}
	}

	public void Deregister(CompGuardPost post)
	{
		posts.Remove(post);
	}

	public CompGuardPost PostFor(Pawn pawn)
	{
		CompGuardPost result = null;
		float num = float.MaxValue;
		for (int i = 0; i < posts.Count; i++)
		{
			CompGuardPost compGuardPost = posts[i];
			if (compGuardPost?.parent != null && compGuardPost.parent.Spawned && compGuardPost.IsAssigned(pawn) && (compGuardPost.Kind != PostKind.Patrol || compGuardPost.order == 0))
			{
				float num2 = pawn.Position.DistanceToSquared(compGuardPost.parent.Position);
				if (num2 < num)
				{
					num = num2;
					result = compGuardPost;
				}
			}
		}
		return result;
	}

	public List<CompGuardPost> Route(PostGroup group, List<CompGuardPost> into)
	{
		into.Clear();
		for (int i = 0; i < posts.Count; i++)
		{
			CompGuardPost compGuardPost = posts[i];
			if (compGuardPost?.parent != null && compGuardPost.parent.Spawned && compGuardPost.Kind == PostKind.Patrol && compGuardPost.group == group)
			{
				into.Add(compGuardPost);
			}
		}
		into.SortBy((CompGuardPost p) => p.order);
		return into;
	}

	public void Reorder(CompGuardPost post, int position)
	{
		if (post == null || post.Kind != PostKind.Patrol)
		{
			return;
		}
		Route(post.group, reorderBuffer);
		if (reorderBuffer.Remove(post))
		{
			if (position < 0)
			{
				position = 0;
			}
			if (position > reorderBuffer.Count)
			{
				position = reorderBuffer.Count;
			}
			reorderBuffer.Insert(position, post);
			for (int i = 0; i < reorderBuffer.Count; i++)
			{
				reorderBuffer[i].order = i;
			}
		}
	}

	public int NextOrderFor(PostGroup group)
	{
		int num = 0;
		for (int i = 0; i < posts.Count; i++)
		{
			CompGuardPost compGuardPost = posts[i];
			if (compGuardPost?.parent != null && compGuardPost.Kind == PostKind.Patrol && compGuardPost.group == group)
			{
				num++;
			}
		}
		return num;
	}

	public CompGuardPost FindConflict(Pawn pawn, CompGuardPost exclude)
	{
		for (int i = 0; i < posts.Count; i++)
		{
			CompGuardPost compGuardPost = posts[i];
			if (compGuardPost != exclude && compGuardPost?.parent != null && compGuardPost.parent.Spawned && (compGuardPost.Kind != PostKind.Patrol || compGuardPost.order == 0) && compGuardPost.IsIndividuallyAssigned(pawn))
			{
				return compGuardPost;
			}
		}
		return null;
	}

	public static void UnassignAcrossGroup(CompGuardPost post, Pawn pawn)
	{
		if (post?.parent?.Map == null)
		{
			post?.Unassign(pawn);
			return;
		}
		GuardRegistry guardRegistry = For(post.parent.Map);
		if (guardRegistry == null)
		{
			post.Unassign(pawn);
			return;
		}
		List<CompGuardPost> list = guardRegistry.posts;
		for (int i = 0; i < list.Count; i++)
		{
			CompGuardPost compGuardPost = list[i];
			if (compGuardPost?.parent != null && compGuardPost.parent.Spawned && compGuardPost.Kind == post.Kind && compGuardPost.group == post.group)
			{
				compGuardPost.Unassign(pawn);
			}
		}
	}
}
