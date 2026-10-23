using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public static class ArmouryStances
{
	private static readonly Dictionary<ThinkTreeDef, HashSet<CombatStance>> byTree = new Dictionary<ThinkTreeDef, HashSet<CombatStance>>();

	private static readonly HashSet<CombatStance> none = new HashSet<CombatStance>();

	public static bool Active(Pawn pawn)
	{
		if (ArmourySettings.stancesEnabled)
		{
			return CompArsenal.For(pawn) != null;
		}
		return false;
	}

	public static CombatStance Effective(CompArsenal comp)
	{
		return comp?.EffectiveStance ?? CombatStance.Hold;
	}

	public static bool HasStances(Pawn pawn)
	{
		return StancesIn(pawn).Count > 0;
	}

	public static bool Offers(Pawn pawn, CombatStance stance)
	{
		HashSet<CombatStance> hashSet = StancesIn(pawn);
		if (hashSet.Count == 0)
		{
			return false;
		}
		if (stance != CombatStance.Hold)
		{
			return hashSet.Contains(stance);
		}
		return true;
	}

	private static HashSet<CombatStance> StancesIn(Pawn pawn)
	{
		ThinkTreeDef thinkTreeDef = pawn?.thinker?.MainThinkTree;
		if (thinkTreeDef?.thinkRoot == null)
		{
			return none;
		}
		if (byTree.TryGetValue(thinkTreeDef, out var value))
		{
			return value;
		}
		value = new HashSet<CombatStance>();
		HashSet<ThinkNode> hashSet = new HashSet<ThinkNode>();
		HashSet<ThinkTreeDef> hashSet2 = new HashSet<ThinkTreeDef> { thinkTreeDef };
		Stack<ThinkNode> stack = new Stack<ThinkNode>();
		stack.Push(thinkTreeDef.thinkRoot);
		while (stack.Count > 0)
		{
			ThinkNode thinkNode = stack.Pop();
			if (thinkNode == null || !hashSet.Add(thinkNode))
			{
				continue;
			}
			if (thinkNode is ThinkNode_ConditionalStance thinkNode_ConditionalStance)
			{
				value.Add(thinkNode_ConditionalStance.stance.Normalise());
			}
			if (thinkNode is ThinkNode_Subtree { treeDef: var treeDef } thinkNode_Subtree && treeDef?.thinkRoot != null && hashSet2.Add(thinkNode_Subtree.treeDef))
			{
				stack.Push(thinkNode_Subtree.treeDef.thinkRoot);
			}
			if (thinkNode is ThinkNode_SubtreesByTag thinkNode_SubtreesByTag && !thinkNode_SubtreesByTag.insertTag.NullOrEmpty())
			{
				List<ThinkTreeDef> allDefsListForReading = DefDatabase<ThinkTreeDef>.AllDefsListForReading;
				for (int i = 0; i < allDefsListForReading.Count; i++)
				{
					ThinkTreeDef thinkTreeDef2 = allDefsListForReading[i];
					if (thinkTreeDef2.insertTag == thinkNode_SubtreesByTag.insertTag && thinkTreeDef2.thinkRoot != null && hashSet2.Add(thinkTreeDef2))
					{
						stack.Push(thinkTreeDef2.thinkRoot);
					}
				}
			}
			List<ThinkNode> subNodes = thinkNode.subNodes;
			if (subNodes != null)
			{
				for (int j = 0; j < subNodes.Count; j++)
				{
					stack.Push(subNodes[j]);
				}
			}
		}
		byTree[thinkTreeDef] = value;
		return value;
	}
}
