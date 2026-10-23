using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

[StaticConstructorOnStartup]
public static class StanceTrees
{
	private static readonly HashSet<ThinkTreeDef> injected;

	private static readonly CombatStance[] AnimalStances;

	static StanceTrees()
	{
		injected = new HashSet<ThinkTreeDef>();
		AnimalStances = new CombatStance[4]
		{
			CombatStance.Engage,
			CombatStance.Hide,
			CombatStance.Brawl,
			CombatStance.Hunt
		};
		if (!ArmourySettings.stancesEnabled)
		{
			return;
		}
		List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			RaceProperties race = allDefsListForReading[i].race;
			if (race != null)
			{
				if (race.IsMechanoid || race.Humanlike)
				{
					Inject(race.thinkTreeMain);
				}
				else if (race.Animal)
				{
					InjectIfAnchored(race.thinkTreeMain, AnimalStances);
				}
			}
		}
	}

	private static void Inject(ThinkTreeDef tree)
	{
		if (tree?.thinkRoot != null && injected.Add(tree))
		{
			if (!TryFindAnchor(tree.thinkRoot, out var parent, out var index))
			{
				Log.Warning("[AsuRim Armoury] No drafted-order node in think tree " + tree.defName + "; stances will not apply to pawns using it.");
			}
			else
			{
				Splice(tree, parent, index, Block());
			}
		}
	}

	private static void InjectIfAnchored(ThinkTreeDef tree, CombatStance[] stances)
	{
		if (tree?.thinkRoot != null && !injected.Contains(tree) && TryFindAnchor(tree.thinkRoot, out var parent, out var index))
		{
			injected.Add(tree);
			Splice(tree, parent, index, MeleeBlock(stances));
		}
	}

	private static void Splice(ThinkTreeDef tree, ThinkNode parent, int index, List<ThinkNode> block)
	{
		parent.subNodes.InsertRange(index, block);
		int startHash = GenText.StableStringHash(tree.defName);
		for (int i = 0; i < block.Count; i++)
		{
			ThinkNode thinkNode = block[i];
			thinkNode.parent = parent;
			SetParents(thinkNode);
			thinkNode.ResolveSubnodesAndRecur();
			foreach (ThinkNode item in thinkNode.ThisAndChildrenRecursive)
			{
				item.ResolveReferences();
				ThinkTreeKeyAssigner.AssignSingleKey(item, startHash);
			}
		}
	}

	private static bool TryFindAnchor(ThinkNode root, out ThinkNode parent, out int index)
	{
		Queue<ThinkNode> queue = new Queue<ThinkNode>();
		queue.Enqueue(root);
		while (queue.Count > 0)
		{
			ThinkNode thinkNode = queue.Dequeue();
			for (int i = 0; i < thinkNode.subNodes.Count; i++)
			{
				if (Contains<JobGiver_Orders>(thinkNode.subNodes[i]))
				{
					parent = thinkNode;
					index = i;
					return true;
				}
			}
			for (int j = 0; j < thinkNode.subNodes.Count; j++)
			{
				if (!Shared(thinkNode.subNodes[j]))
				{
					queue.Enqueue(thinkNode.subNodes[j]);
				}
			}
		}
		parent = null;
		index = -1;
		return false;
	}

	private static bool Contains<T>(ThinkNode node) where T : ThinkNode
	{
		if (node is T)
		{
			return true;
		}
		for (int i = 0; i < node.subNodes.Count; i++)
		{
			if (!Shared(node.subNodes[i]) && Contains<T>(node.subNodes[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static bool Shared(ThinkNode node)
	{
		if (!(node is ThinkNode_Subtree))
		{
			return node is ThinkNode_SubtreesByTag;
		}
		return true;
	}

	private static void SetParents(ThinkNode node)
	{
		for (int i = 0; i < node.subNodes.Count; i++)
		{
			node.subNodes[i].parent = node;
			SetParents(node.subNodes[i]);
		}
	}

	private static List<ThinkNode> Block()
	{
		List<ThinkNode> list = new List<ThinkNode>
		{
			new JobGiver_Retreat(),
			new JobGiver_PickupDroppedOffHand(),
			new JobGiver_Bodyguard(),
			Branch(CombatStance.Engage, new JobGiver_StanceEngage(), approach: true),
			Branch(CombatStance.Hide, new JobGiver_StanceHide(), approach: false),
			Branch(CombatStance.Brawl, new JobGiver_StanceBrawl(), approach: true),
			Branch(CombatStance.Hunt, new JobGiver_StanceHunt(), approach: true),
			Branch(CombatStance.VIP, new JobGiver_StanceVIP(), approach: true),
			Branch(CombatStance.Immobilise, new JobGiver_StanceEngage(), approach: true),
			Branch(CombatStance.Suppress, new JobGiver_StanceEngage(), approach: true),
			Branch(CombatStance.Covering, new JobGiver_StanceCovering(), approach: true),
			Branch(CombatStance.Medic, new JobGiver_StanceMedic(), approach: true)
		};
		ThinkNode_Priority item = new ThinkNode_Priority
		{
			subNodes = 
			{
				(ThinkNode)new JobGiver_MoveToStandable(),
				(ThinkNode)new JobGiver_StanceAbilities(),
				(ThinkNode)new JobGiver_StanceBreach(),
				(ThinkNode)new JobGiver_StanceHunt(),
				(ThinkNode)new JobGiver_StanceApproach(),
				(ThinkNode)new JobGiver_StanceIdle()
			}
		};
		ThinkNode_ConditionalStance thinkNode_ConditionalStance = new ThinkNode_ConditionalStance
		{
			stance = CombatStance.Breach
		};
		thinkNode_ConditionalStance.subNodes.Add(item);
		list.Add(thinkNode_ConditionalStance);
		return list;
	}

	private static List<ThinkNode> MeleeBlock(CombatStance[] stances)
	{
		List<ThinkNode> list = new List<ThinkNode>();
		for (int i = 0; i < stances.Length; i++)
		{
			switch (stances[i])
			{
			case CombatStance.Engage:
				list.Add(Branch(CombatStance.Engage, new JobGiver_StanceEngage(), approach: true));
				break;
			case CombatStance.Hide:
				list.Add(Branch(CombatStance.Hide, new JobGiver_StanceHide(), approach: false));
				break;
			case CombatStance.Brawl:
				list.Add(Branch(CombatStance.Brawl, new JobGiver_StanceBrawl(), approach: true));
				break;
			case CombatStance.Hunt:
				list.Add(Branch(CombatStance.Hunt, new JobGiver_StanceHunt(), approach: true));
				break;
			}
		}
		return list;
	}

	private static ThinkNode Branch(CombatStance stance, ThinkNode giver, bool approach)
	{
		ThinkNode_Priority thinkNode_Priority = new ThinkNode_Priority();
		thinkNode_Priority.subNodes.Add(new JobGiver_MoveToStandable());
		if (stance != CombatStance.Hide)
		{
			thinkNode_Priority.subNodes.Add(new JobGiver_StanceAbilities());
		}
		thinkNode_Priority.subNodes.Add(giver);
		if (approach)
		{
			thinkNode_Priority.subNodes.Add(new JobGiver_StanceApproach());
		}
		thinkNode_Priority.subNodes.Add(new JobGiver_StanceIdle());
		return new ThinkNode_ConditionalStance
		{
			stance = stance,
			subNodes = { (ThinkNode)thinkNode_Priority }
		};
	}
}
